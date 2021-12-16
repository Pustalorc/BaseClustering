using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Buildables;
using Pustalorc.Plugins.BaseClustering.API.Delegates;
using Pustalorc.Plugins.BaseClustering.API.Patches;
using Pustalorc.Plugins.BaseClustering.API.Utilities;
using Pustalorc.Plugins.BaseClustering.Config;
using SDG.Unturned;
using Steamworks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Pustalorc.Plugins.BaseClustering.API.BaseClusters
{
    /// <summary>
    /// A directory that keeps track of all <see cref="BaseCluster"/>s.
    /// </summary>
    public sealed class BaseClusterDirectory
    {
        /// <summary>
        /// This event is raised when <see cref="GenerateAndLoadAllClusters"/> has finished executing.
        /// </summary>
        [UsedImplicitly]
        public event VoidDelegate? OnClustersGenerated;

        /// <summary>
        /// This event is raised whenever a new cluster is added.
        /// </summary>
        [UsedImplicitly]
        public event ClusterChange? OnClusterAdded;

        /// <summary>
        /// This event is raised whenever a cluster is removed.
        /// </summary>
        [UsedImplicitly]
        public event ClusterChange? OnClusterRemoved;

        private readonly BaseClusteringPlugin m_Plugin;
        private readonly BaseClusteringPluginConfiguration m_PluginConfiguration;
        private readonly BuildableDirectory m_BuildableDirectory;
        private readonly ConcurrentBag<BaseCluster> m_ClusterPool;
        private readonly List<BaseCluster> m_Clusters;
        private string m_SaveFilePath;

        private BaseCluster? m_GlobalCluster;
        private int m_InstanceIds;

        /// <summary>
        /// Gets a copied <see cref="IReadOnlyCollection{BaseCluster}"/> of all the clusters tracked.
        /// </summary>
        /// <remarks>
        /// This copied collection includes the global cluster from <see cref="GetOrCreateGlobalCluster"/>.
        /// </remarks>
        public IReadOnlyCollection<BaseCluster> Clusters =>
            new ReadOnlyCollection<BaseCluster>(m_Clusters.Concat(new[] { GetOrCreateGlobalCluster() }).ToList());

        /// <summary>
        /// Creates a new instance of the BaseCluster Directory.
        /// </summary>
        /// <param name="plugin">The instance of the plugin.</param>
        /// <param name="pluginConfiguration">The configuration of the plugin.</param>
        /// <param name="buildableDirectory">The buildable directory, which should've been initialized beforehand.</param>
        public BaseClusterDirectory(BaseClusteringPlugin plugin, BaseClusteringPluginConfiguration pluginConfiguration,
            BuildableDirectory buildableDirectory)
        {
            m_Plugin = plugin;
            m_PluginConfiguration = pluginConfiguration;
            m_BuildableDirectory = buildableDirectory;
            m_ClusterPool = new ConcurrentBag<BaseCluster>();
            m_Clusters = new List<BaseCluster>();
            m_SaveFilePath = ServerSavedata.directory + "/" + Provider.serverID + "/Level/" +
                             (Level.info?.name ?? "Washington") + "/Bases.dat";

            PatchBuildableTransforms.OnBuildableTransformed += BuildableTransformed;
            buildableDirectory.OnBuildablesAdded += BuildablesSpawned;
            buildableDirectory.OnBuildablesRemoved += BuildablesDestroyed;
            SaveManager.onPostSave += Save;
        }

        internal void LevelLoaded()
        {
            m_SaveFilePath = ServerSavedata.directory + "/" + Provider.serverID + "/Level/" +
                             Level.info.name + "/Bases.dat";
            GenerateAndLoadAllClusters();

            while (m_ClusterPool.Count < 25)
                m_ClusterPool.Add(new BaseCluster(m_PluginConfiguration, this, m_InstanceIds++));
        }

        internal void Unload()
        {
            PatchBuildableTransforms.OnBuildableTransformed -= BuildableTransformed;
            m_BuildableDirectory.OnBuildablesAdded -= BuildablesSpawned;
            m_BuildableDirectory.OnBuildablesRemoved -= BuildablesDestroyed;
            SaveManager.onPostSave -= Save;
            Save();
        }

        internal void GenerateAndLoadAllClusters(bool loadSaveFile = true)
        {
            var stopwatch = Stopwatch.StartNew();

            var allBuildables = BuildableDirectory.GetBuildables(includePlants: true).ToList();
            Logging.Write(m_Plugin,
                $"Loaded {allBuildables.Count} buildables from the map. Took {stopwatch.ElapsedMilliseconds}ms",
                ConsoleColor.Cyan);

            foreach (var c in m_Clusters)
                Return(c);

            var successfulLoad = false;
            if (loadSaveFile && LevelSavedata.fileExists("/Bases.dat"))
                successfulLoad = LoadClusters(allBuildables);

            if (!successfulLoad)
            {
                Logging.Write(m_Plugin,
                    "Generating new clusters. This can take a LONG time. How long will depend on the following factors (but not limited to): CPU usage, CPU cores/threads, Buildables in the map. This generation only needs to be ran once from raw.");
                m_Clusters.AddRange(ClusterElements(allBuildables, true));
            }

            stopwatch.Stop();
            Logging.Write(m_Plugin,
                $"Clusters Loaded: {Clusters.Count}. Took {stopwatch.ElapsedMilliseconds}ms.",
                ConsoleColor.Cyan);

            OnClustersGenerated?.Invoke();
        }

        /// <summary>
        /// Saves the current data in <see cref="Clusters"/>.
        /// </summary>
        public void Save()
        {
            m_BuildableDirectory.WaitDestroyHandle();
            var river = new RiverExpanded(m_SaveFilePath);
            river.WriteInt32(m_BuildableDirectory.Buildables.Count);
            var clusters = Clusters;
            river.WriteInt32(clusters.Count);
            foreach (var cluster in clusters)
            {
                river.WriteInt32(cluster.InstanceId);
                river.WriteBoolean(cluster.IsGlobalCluster);
                river.WriteInt32(cluster.Buildables.Count);
                foreach (var build in cluster.Buildables)
                {
                    river.WriteUInt32(build.InstanceId);
                    river.WriteBoolean(build is StructureBuildable);
                }
            }

            river.CloseRiver();
            m_BuildableDirectory.RestartBackgroundWorker();
        }

        private bool LoadClusters(IEnumerable<Buildable> allBuildables)
        {
            var bases = new List<BaseCluster>();

            foreach (var c in m_Clusters)
                Return(c);

            try
            {
                var timer = Stopwatch.StartNew();
                var river = new RiverExpanded(m_SaveFilePath);
                var allBuilds = allBuildables.ToList();
                var structures = allBuilds.OfType<StructureBuildable>().ToDictionary(k => k.InstanceId);
                var barricades = allBuilds.OfType<BarricadeBuildable>().ToDictionary(k => k.InstanceId);

                var buildableCount = river.ReadInt32();

                if (allBuilds.Count != buildableCount)
                {
                    Logging.Write(m_Plugin,
                        "Warning! Buildable count doesn't match saved count! Buildable save data was most likely modified or lost during server downtime. Clusters will be now rebuilt.",
                        ConsoleColor.Yellow);
                    return false;
                }

                var clusterCount = river.ReadInt32();
                var logRate = Math.Floor(clusterCount * 0.085);

                Logging.Write(m_Plugin,
                    $"Loading saved clusters... 0% [0/{clusterCount}] {timer.ElapsedMilliseconds}ms",
                    ConsoleColor.Cyan);

                for (var i = 0; i < clusterCount; i++)
                {
                    var builds = new List<Buildable>();
                    // Restore of instanceId is needed to maintain something unique to each cluster across restarts.
                    var instanceId = river.ReadInt32();
                    var global = river.ReadBoolean();

                    var buildCount = river.ReadInt32();
                    for (var o = 0; o < buildCount; o++)
                    {
                        var buildInstanceId = river.ReadUInt32();
                        var isStructure = river.ReadBoolean();
                        var build = isStructure
                            ? (Buildable)structures[buildInstanceId]
                            : barricades[buildInstanceId];

                        if (build == null)
                        {
                            Logging.Write(m_Plugin,
                                $"Warning! Buildable with InstanceId {buildInstanceId} [isStructure: {isStructure}] not found! Save data was most likely modified or lost during server downtime. Clusters will be now rebuilt.",
                                ConsoleColor.Yellow);
                            river.CloseRiver();
                            return false;
                        }

                        builds.Add(build);
                    }

                    if (global)
                    {
                        if (m_GlobalCluster != null)
                        {
                            m_GlobalCluster.AddBuildables(builds);
                        }
                        else
                        {
                            var cluster = CreateCluster(instanceId, true);
                            cluster.AddBuildables(builds);
                            m_GlobalCluster = cluster;
                        }
                    }
                    else
                    {
                        var cluster = GetOrCreatePooledCluster();
                        cluster.AddBuildables(builds);
                        bases.Add(cluster);
                    }

                    if ((i + 1) % logRate == 0)
                        Logging.Write(m_Plugin,
                            $"Loading saved clusters... {Math.Ceiling((i + 1) / (double)clusterCount * 100)}% [{i + 1}/{clusterCount}] {timer.ElapsedMilliseconds}ms",
                            ConsoleColor.Cyan);
                }

                m_Clusters.AddRange(bases);

                if (Clusters.Count > 0)
                    m_InstanceIds = Clusters.Max(k => k.InstanceId) + 1;

                for (var i = 0; i < m_InstanceIds; i++)
                {
                    if (Clusters.Any(k => k.InstanceId == i) || m_ClusterPool.Any(k => k.InstanceId == i))
                        continue;

                    m_ClusterPool.Add(CreateCluster(i));
                }

                timer.Stop();
                return true;
            }
            catch (Exception ex)
            {
                Logging.Write(m_Plugin,
                    $"Warning! An exception was thrown when attempting to load the save file. Assuming the data is corrupted. Clusters will be now rebuilt. Exception: {ex}",
                    ConsoleColor.Yellow);

                foreach (var b in bases)
                    Return(b);

                return false;
            }
        }

        /// <summary>
        /// Gets a <see cref="BaseCluster"/> from the pool.
        /// <br/>
        /// If a <see cref="BaseCluster"/> isn't available from the pool, a new instance will be created and provided.
        /// </summary>
        /// <returns>An instance of type <see cref="BaseCluster"/>.</returns>
        public BaseCluster GetOrCreatePooledCluster()
        {
            return m_ClusterPool.TryTake(out var baseCluster)
                ? baseCluster
                : CreateCluster(m_InstanceIds++);
        }

        private BaseCluster CreateCluster(int instanceId, bool globalCluster = false)
        {
            return new BaseCluster(m_PluginConfiguration, this, instanceId, globalCluster);
        }

        /// <summary>
        /// Returns and resets a <see cref="BaseCluster"/> to the pool.
        /// </summary>
        /// <param name="baseCluster">The <see cref="BaseCluster"/> to reset and return to the pool.</param>
        public void Return(BaseCluster? baseCluster)
        {
            if (baseCluster == null)
                return;

            baseCluster.Reset();
            m_Clusters.Remove(baseCluster);

            if (baseCluster.IsGlobalCluster)
                return;

            m_ClusterPool.Add(baseCluster);
            OnClusterRemoved?.Invoke(baseCluster);
        }

        /// <summary>
        /// Gets the global <see cref="BaseCluster"/>.
        /// <br/>
        /// If there's no global <see cref="BaseCluster"/> available, a new instance will be created and provided.
        /// </summary>
        /// <returns>An instance of <see cref="BaseCluster"/>.</returns>
        public BaseCluster GetOrCreateGlobalCluster()
        {
            return m_GlobalCluster ??= CreateCluster(m_InstanceIds++, true);
        }

        /// <summary>
        /// Generates a new <see cref="IEnumerable{BaseCluster}"/> with all the clusters generated from the inputs.
        /// </summary>
        /// <param name="buildables">The <see cref="IEnumerable{Buildable}"/> to cluster.</param>
        /// <param name="needLogging">Should progress be logged to console whilst it clusters.</param>
        /// <returns>An <see cref="IEnumerable{BaseCluster}"/> with all the generated clusters</returns>
        public IEnumerable<BaseCluster> ClusterElements(IEnumerable<Buildable> buildables, bool needLogging = false)
        {
            // Start a new stopwatch. This will be used to log how long the program is taking with each step.
            var stopwatch = Stopwatch.StartNew();
            // Initialize an empty sample output for this method.
            var output = new List<BaseCluster>();
            // Set constants of squared distance. This will be used on distance checks.
            var maxStructureDistance = Mathf.Pow(m_PluginConfiguration.MaxDistanceBetweenStructures, 2);
            var maxBarricadeDistance = Mathf.Pow(m_PluginConfiguration.MaxDistanceToConsiderPartOfBase, 2);
            // Set a couple variables that are used for logging.
            var currentMultiplier = 0;
            var currentCount = 0;

            // Get all the buildables to cluster. Anything planted should NOT be clustered.
            var buildablesToCluster = buildables.Where(k => !k.IsPlanted).ToList();
            // Get the count of buildables to cluster. This will be used for logging.
            var totalBuildablesToCluster = buildablesToCluster.Count;
            var logRate = Math.Floor(totalBuildablesToCluster * 0.085);

            // Get all the structures to cluster from all the buildables that are being clustered.
            var structuresToCluster = buildablesToCluster.OfType<StructureBuildable>().ToList();
            // Get all the barricades to cluster from all the buildables that are being clustered.
            var barricadesToCluster = buildablesToCluster.OfType<BarricadeBuildable>().ToList();

            // A cluster is made by having at least one Structure. If we run out of structures to cluster, then the rest will be clustered in the global cluster.
            while (structuresToCluster.Count > 0)
            {
                // Create a variable to store all the structures of the cluster.
                var structuresOfCluster = new List<StructureBuildable>();
                // Create a variable to store all the buildables of the cluster.
                var buildablesOfCluster = new List<Buildable>();

                // Pick a random structure (floor, pillar, wall, etc.)
                var targetStructure = structuresToCluster[Random.Range(0, structuresToCluster.Count)];
                // Remove the picked structure from the toCluster list.
                structuresToCluster.Remove(targetStructure);
                // Add the picked structure to the final buildables of cluster list.
                structuresOfCluster.Add(targetStructure);

                // Loop through buildablesOfCluster. Each element should only be checked against all others once.
                for (var i = 0; i < structuresOfCluster.Count; i++)
                {
                    // Get the element we are currently checking.
                    var s = structuresOfCluster[i];

                    // Check which of all the structures in the world we can add here.
                    var toAdd = structuresToCluster
                        .Where(k => (k.Position - s.Position).sqrMagnitude <= maxStructureDistance).ToList();
                    // Add all those structures to the cluster.
                    structuresOfCluster.AddRange(toAdd);
                    // Remove all those structures from the main list.
                    structuresToCluster.RemoveAll(toAdd.Contains);
                }

                // Barricades are simpler to cluster than structures. Barricades are only considered part of the cluster if there's a structure within range.
                var barricadesToAdd = barricadesToCluster.Where(next =>
                        structuresOfCluster.Exists(k =>
                            (next.Position - k.Position).sqrMagnitude <= maxBarricadeDistance))
                    .ToList();
                // Add all the barricades that are within range of one of the structures of this cluster.
                buildablesOfCluster.AddRange(barricadesToAdd);
                // Finally, remove all the barricades from the main list that we added to the cluster.
                barricadesToCluster.RemoveAll(barricadesToAdd.Contains);

                // Combine all the buildables into one list.
                buildablesOfCluster.AddRange(structuresOfCluster);
                // Get or create a pooled cluster so we can define the cluster.
                var cluster = GetOrCreatePooledCluster();
                // Add all the combined buildables to this cluster.
                cluster.AddBuildables(buildablesOfCluster);
                // Add this cluster to the output list.
                output.Add(cluster);

                // Finally, check if we need logging, and if we are ready to log it.
                currentCount += cluster.Buildables.Count;
                if (!needLogging || !(currentCount / logRate > currentMultiplier)) continue;

                currentMultiplier++;
                Logging.Write(m_Plugin,
                    $"Generating new clusters... {Math.Ceiling(currentCount / (double)totalBuildablesToCluster * 100)}% [{currentCount}/{totalBuildablesToCluster}] {stopwatch.ElapsedMilliseconds}ms",
                    ConsoleColor.Cyan);
            }

            // Once all the structures have been clustered, check if we have any remaining barricades that have not been clustered.
            var remainingBarricadeCount = barricadesToCluster.Count;
            if (remainingBarricadeCount > 0)
            {
                // If we do have barricades that have not been clustered, get or create a global cluster.
                var globalCluster = GetOrCreateGlobalCluster();
                // And add all those barricades to that global cluster.
                globalCluster.AddBuildables(barricadesToCluster);
            }

            // Finally, we should make sure we are logging the 100% message with this check, should logging actually be needed.

            // This invert is dumb, as we still need to return output. All we are doing is adding a visually earlier return, which makes 0 sense to do.
            // ReSharper disable once InvertIf
            if (needLogging)
            {
                var finalBuildCount = output.Sum(k => k.Buildables.Count) + remainingBarricadeCount;
                Logging.Write(m_Plugin,
                    $"Generating new clusters... {Math.Ceiling(finalBuildCount / (double)totalBuildablesToCluster * 100)}% [{finalBuildCount}/{totalBuildablesToCluster}] {stopwatch.ElapsedMilliseconds}ms",
                    ConsoleColor.Cyan);
            }

            return output;
        }

        /// <summary>
        /// Registers a new <see cref="BaseCluster"/> to <see cref="Clusters"/>.
        /// </summary>
        /// <param name="cluster">The <see cref="BaseCluster"/> to register.</param>
        public void RegisterCluster(BaseCluster cluster)
        {
            m_Clusters.Add(cluster);
            OnClusterAdded?.Invoke(cluster);
        }

        /// <summary>
        /// Finds the best cluster for a specific buildable to be placed in.
        /// </summary>
        /// <param name="target">The buildable to find the best clusters for.</param>
        /// <returns>
        /// <see langword="null"/> if no best cluster is available.
        /// <br/>
        /// An instance of <see cref="BaseCluster"/> if a best cluster is available.
        /// </returns>
        public BaseCluster? FindBestCluster(Buildable target)
        {
            return FindBestClusters(target).FirstOrDefault();
        }

        /// <summary>
        /// Finds the best clusters for a specific buildable to be placed in.
        /// </summary>
        /// <param name="target">The buildable to find the best clusters for.</param>
        /// <returns>
        /// An <see cref="IEnumerable{BaseCluster}"/> with the best <see cref="BaseCluster"/>s for the buildable.
        /// If no best clusters are found, <see cref="IEnumerable{BaseCluster}"/> will be empty.
        /// </returns>
        public IEnumerable<BaseCluster> FindBestClusters(Buildable target)
        {
            return Clusters.Where(k => k.IsWithinRange(target))
                .OrderBy(k => (k.AverageCenterPosition - target.Position).sqrMagnitude);
        }

        /// <summary>
        /// Finds the best cluster within range of a specific position
        /// </summary>
        /// <param name="target">The position to check within range.</param>
        /// <returns>
        /// <see langword="null"/> if no best cluster is available.
        /// <br/>
        /// An instance of <see cref="BaseCluster"/> if a best cluster is available.
        /// </returns>
        [UsedImplicitly]
        public BaseCluster? FindBestCluster(Vector3 target)
        {
            return FindBestClusters(target).FirstOrDefault();
        }

        /// <summary>
        /// Finds the best clusters within range of a specific position.
        /// </summary>
        /// <param name="target">The position to check within range.</param>
        /// <returns>
        /// An <see cref="IEnumerable{BaseCluster}"/> with the best <see cref="BaseCluster"/>s for the buildable.
        /// If no best clusters are found, <see cref="IEnumerable{BaseCluster}"/> will be empty.
        /// </returns>
        public IEnumerable<BaseCluster> FindBestClusters(Vector3 target)
        {
            return Clusters.Where(k => k.IsWithinRange(target))
                .OrderBy(k => (k.AverageCenterPosition - target).sqrMagnitude);
        }

        private void BuildablesDestroyed(IEnumerable<Buildable> buildables)
        {
            var builds = buildables.ToList();

            foreach (var cluster in Clusters.ToList())
            {
                if (builds.Count == 0)
                    return;

                cluster.RemoveBuildables(builds);
            }
        }

        private void BuildableTransformed(Buildable buildable)
        {
            var builds = new[] { buildable };
            BuildablesDestroyed(builds);
            BuildablesSpawned(builds);
        }

        private void BuildablesSpawned(IEnumerable<Buildable> buildables)
        {
            var gCluster = GetOrCreateGlobalCluster();

            foreach (var buildable in buildables)
            {
                if (buildable.IsPlanted) return;

                // On spawning, check if its a barricade
                if (buildable is BarricadeBuildable)
                {
                    // Find the best cluster for this barricade.
                    var bestCluster = FindBestCluster(buildable);

                    // If we find a best cluster, add it on it.
                    if (bestCluster != null)
                    {
                        bestCluster.AddBuildable(buildable);
                        return;
                    }

                    // If we don't, add it to the global cluster.
                    gCluster.AddBuildable(buildable);
                    return;
                }

                // Otherwise, if its a structure, find all the clusters where it'd make a good target, and exclude any global clusters from the result.
                var bestClusters = FindBestClusters(buildable).ToList();

                switch (bestClusters.Count)
                {
                    // If there's no results, create a new non-global cluster for this new base.
                    case 0:
                        var cluster = GetOrCreatePooledCluster();
                        cluster.AddBuildable(buildable);
                        RegisterCluster(cluster);
                        cluster.StealFromGlobal(gCluster);
                        return;
                    // If there's exactly 1 cluster found, simply add it to that cluster.
                    case 1:
                        cluster = bestClusters.First();
                        cluster.AddBuildable(buildable);
                        cluster.StealFromGlobal(gCluster);
                        return;

                    // However, if there's more than 1 cluster, select every single buildable from all found clusters.
                    default:
                        var allBuilds = bestClusters.SelectMany(k => k.Buildables).ToList();

                        // Make sure to include the buildable we spawned in that set.
                        allBuilds.Add(buildable);

                        // For all the found best clusters, we can now un-register them, as they are no longer needed.
                        foreach (var c in bestClusters)
                            Return(c);

                        // And ask the clustering tool to generate new clusters, and populate the global cluster.
                        var newClusters = ClusterElements(allBuilds);

                        // New clusters can be safely added now.
                        foreach (var c in newClusters)
                        {
                            RegisterCluster(c);
                            c.StealFromGlobal(c);
                        }

                        return;
                }
            }
        }

        /// <summary>
        /// Retrieves all clusters that have the specified <paramref name="player"/> as the most common owner.
        /// </summary>
        /// <param name="player">The player to use for the search as the most common owner.</param>
        /// <returns>
        /// An <see cref="IEnumerable{BaseCluster}"/> holding all the clusters that this player is deemed "most common owner" of.
        /// </returns>
        [UsedImplicitly]
        public IEnumerable<BaseCluster> GetMostOwnedClusters(CSteamID player)
        {
            return GetClustersWithFilter(k => k.CommonOwner == player.m_SteamID);
        }

        /// <summary>
        /// Retrieves all clusters that satisfy the custom filter.
        /// </summary>
        /// <param name="filter">An anonymous function that takes BaseCluster as parameter and returns bool.</param>
        /// <returns>
        /// An <see cref="IEnumerable{BaseCluster}"/> that satisfies the filter.
        /// </returns>
        public IEnumerable<BaseCluster> GetClustersWithFilter(Func<BaseCluster, bool> filter)
        {
            return Clusters.Where(filter);
        }

        /// <summary>
        /// Gets the cluster that contains the element with the provided model.
        /// </summary>
        /// <param name="model">The model of the buildable within a cluster.</param>
        /// <returns>
        /// <see langword="null"/> if no cluster is found.
        /// <br/>
        /// An instance of <see cref="BaseCluster"/> if a cluster exists.
        /// </returns>
        [UsedImplicitly]
        public BaseCluster? GetClusterWithElement(Transform model)
        {
            return Clusters.FirstOrDefault(k => k.Buildables.Any(l => l.Model == model));
        }

        /// <summary>
        /// Gets the cluster that contains the element with the provided position.
        /// </summary>
        /// <param name="instanceId">The instanceId of the buildable within a cluster.</param>
        /// <param name="isStructure">If the instanceId belongs to a structure or a barricade.</param>
        /// <returns>
        /// <see langword="null"/> if no cluster is found.
        /// <br/>
        /// An instance of <see cref="BaseCluster"/> if a cluster exists.
        /// </returns>
        [UsedImplicitly]
        public BaseCluster? GetClusterWithElement(uint instanceId, bool isStructure)
        {
            return Clusters.FirstOrDefault(k =>
            {
                var builds = k.Buildables.AsEnumerable();

                if (builds == null)
                    return false;

                if (isStructure)
                    builds = builds.OfType<StructureBuildable>();
                else
                    builds = builds.OfType<BarricadeBuildable>();

                return builds.Any(l => l.InstanceId == instanceId);
            });
        }

        /// <summary>
        /// Gets the cluster that contains the element with the provided buildable instance.
        /// </summary>
        /// <param name="buildable">The buildable within a cluster.</param>
        /// <returns>
        /// <see langword="null"/> if no cluster is found.
        /// <br/>
        /// An instance of <see cref="BaseCluster"/> if a cluster exists.
        /// </returns>
        [UsedImplicitly]
        public BaseCluster? GetClusterWithElement(Buildable buildable)
        {
            return Clusters.FirstOrDefault(k => k.Buildables.Contains(buildable));
        }
    }
}