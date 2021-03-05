using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API;
using Pustalorc.Plugins.BaseClustering.API.Buildables;
using Pustalorc.Plugins.BaseClustering.API.Delegates;
using Pustalorc.Plugins.BaseClustering.API.Patches;
using Pustalorc.Plugins.BaseClustering.API.Utilities;
using Pustalorc.Plugins.BaseClustering.Config;
using Rocket.API.Collections;
using Rocket.Core.Plugins;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering
{
    public sealed class BaseClusteringPlugin : RocketPlugin<BaseClusteringPluginConfiguration>
    {
        public static BaseClusteringPlugin Instance { get; private set; }
        public static event VoidDelegate OnBuildablesProcessed;
        public event VoidDelegate OnClustersCleared;
        public event ClusterChange OnClusterAdded;
        public event ClusterChange OnClusterRemoved;

        private ObservableCollection<BaseCluster> m_Clusters;
        private ConcurrentQueue<int> m_FreeInstanceIds;
        private int m_InstanceCount;
        private readonly object m_InstanceCountLock = new object();
        private Harmony m_Harmony;

        public ClusteringTool ClusteringTool { get; private set; }

        public ObservableCollection<BaseCluster> Clusters
        {
            get => m_Clusters;
            private set
            {
                if (m_Clusters == null)
                {
                    m_Clusters = value;
                    m_Clusters.CollectionChanged += ClustersChanged;
                    return;
                }

                lock (Clusters)
                {
                    m_Clusters.Clear();
                    foreach (var cluster in value)
                        m_Clusters.Add(cluster);
                }
            }
        }

        [NotNull]
        public override TranslationList DefaultTranslations => new TranslationList
        {
            {
                "clusters_regen_warning",
                "WARNING! This operation can take a long amount of time! The more buildables in the map the longer it will take! Please see console for when it is done."
            },
            {"cannot_be_executed_from_console", "That command cannot be executed from console with those arguments."},
            {"not_available", "N/A"},
            {
                "build_count",
                "There are a total of {0} builds. Specific Item: {1}, Radius: {2}, Player: {3}, Planted Barricades Included: {4}, Filter by Barricades: {5}, Filter by Structures: {6}"
            },
            {"cluster_count", "There are a total of {0} clusters. Specific Item: {1}, Radius: {2}, Player: {3}"},
            {
                "cannot_teleport_no_builds",
                "Cannot teleport anywhere, no buildables found with the following filters. Specific Item: {0}, Player: {1}, Planted Barricades Included: {2}, Filter by Barricades: {3}, Filter by Structures: {4}"
            },
            {
                "cannot_teleport_builds_too_close",
                "Cannot teleport anywhere, all buildables with the specified filters are too close. Specific Item: {0}, Player: {1}, Planted Barricades Included: {2}, Filter by Barricades: {3}, Filter by Structures: {4}"
            },
            {
                "cannot_teleport_no_clusters",
                "Cannot teleport anywhere, no clusters found with the following filters. Player: {0}"
            },
            {"top_builder_format", "At number {0}, {1} with {2} buildables!"},
            {"top_cluster_format", "At number {0}, {1} with {2} clusters!"},
            {"action_cancelled", "The wreck action was cancelled."},
            {"no_action_queued", "There is no wreck action queued."},
            {"cannot_wreck_no_clusters", "There are no clusters selected, so nothing can be wrecked."},
            {"wrecked_clusters", "Told clusters to self-destruct. This might take a while. Total of {0} clusters affected. Specific Item: {1}, Radius: {2}, Player: {3}"},
            {
                "wreck_clusters_action_queued",
                "Queued a wreck clusters action. Confirm with /wc confirm. Player: {0}, Specific Item: {1}, Radius: {2}, "
            },
            {
                "wreck_clusters_action_queued_new",
                "Discarded previous queued action and queued a new wreck clusters action. Confirm with /wc confirm. Player: {0}, Specific Item: {1}, Radius: {2}, "
            },
            {"cannot_wreck_no_builds", "There are no buildables selected, so nothing can be wrecked."},
            {
                "wrecked",
                "Queued a total of {0} buildables to be destroyed. This will take a while. Check console for completion. Specific Item: {1}, Radius: {2}, Player: {3}, Planted Barricades Included: {4}, Filter by Barricades: {5}, Filter by Structures: {6}"
            },
            {"wreck_completed", "Wreck queue was finished and all queued buildables were destroyed."},
            {
                "wreck_action_queued",
                "Queued a wreck action. Confirm with /w confirm. Specific Item: {0}, Radius: {1}, Player: {2}, Planted Barricades Included: {3}, Filter by Barricades: {4}, Filter by Structures: {5}"
            },
            {
                "wreck_action_queued_new",
                "Discarded previous queued action and queued a new wreck action. Confirm with /w confirm. Specific Item: {0}, Radius: {1}, Player: {2}, Planted Barricades Included: {3}, Filter by Barricades: {4}, Filter by Structures: {5}"
            },
            {
                "no_vehicle_found",
                "Couldn't find a vehicle in the direction you're looking, or you are too far away from one. Maximum distance is 10 units."
            },
            {
                "vehicle_dead",
                "The vehicle you are looking at is destroyed and cannot be wrecked. Please look at a vehicle that isn't destroyed."
            },
            {
                "vehicle_no_plant",
                "The vehicle appears to have no assigned barricades to it, please make sure that it has barricades before asking to wreck them."
            },
            {"vehicle_wreck", "Wrecked buildables from {0} [{1}]. Instance ID: {2}, Owner: {3}"},
            {"not_enough_args", "You need more arguments to use this command."}
        };

        protected override void Load()
        {
            Instance = this;
            ClusteringTool = new ClusteringTool(this);
            m_Harmony = new Harmony("xyz.pustalorc.baseClustering");
            m_Harmony.PatchAll();
            m_FreeInstanceIds = new ConcurrentQueue<int>();

            if (Level.isLoaded)
                OnLevelLoaded(0);
            else
                Level.onLevelLoaded += OnLevelLoaded;

            Logging.PluginLoaded(this);
        }

        protected override void Unload()
        {
            PatchBuildableTransforms.OnBuildableTransformed -= BuildableTransformed;
            PatchBuildableSpawns.OnBuildableSpawned -= BuildableSpawned;
            PatchBuildablesDestroy.OnBuildableDestroyed -= BuildableDestroyed;
            Provider.onCommenceShutdown -= ForceDataSave;
            SaveManager.onPostSave -= Save;

            m_Harmony.UnpatchAll();
            m_Harmony = null;
            Instance = null;

            Logging.PluginUnloaded(this);
        }

        private void OnLevelLoaded(int level)
        {
            PatchBuildableTransforms.OnBuildableTransformed += BuildableTransformed;
            PatchBuildableSpawns.OnBuildableSpawned += BuildableSpawned;
            PatchBuildablesDestroy.OnBuildableDestroyed += BuildableDestroyed;
            Provider.onCommenceShutdown += ForceDataSave;
            SaveManager.onPostSave += Save;

            GenerateAndLoadAllClusters();
            OnBuildablesProcessed?.Invoke();
        }

        internal void GenerateAndLoadAllClusters(bool loadSaveFile = true)
        {
            var stopwatch = Stopwatch.StartNew();

            var allBuildables = BuildableCollection.GetBuildables(useProcessedData: false);
            Logging.Write("BaseClustering",
                $"Loaded {allBuildables.Count()} buildables from the map. Took {stopwatch.ElapsedMilliseconds}ms",
                ConsoleColor.Cyan);

            var successfulLoad = false;
            if (loadSaveFile && LevelSavedata.fileExists("/Bases.dat"))
                successfulLoad = LoadClusters(allBuildables);

            if (!successfulLoad)
                Clusters = new ObservableCollection<BaseCluster>(ClusteringTool.ClusterElements(allBuildables, true));

            stopwatch.Stop();
            Logging.Write("BaseClustering",
                $"Clusters Loaded: {Clusters.Count}. Took {stopwatch.ElapsedMilliseconds}ms.", ConsoleColor.Cyan);
        }

        // ReSharper disable once FunctionComplexityOverflow
        // TODO: Should solve complexity.
        private bool LoadClusters([NotNull] IEnumerable<Buildable> allBuildables)
        {
            lock (m_InstanceCountLock)
            {
                try
                {
                    var timer = Stopwatch.StartNew();
                    var bases = new List<BaseCluster>();
                    var river = new RiverExpanded(ServerSavedata.directory + "/" + Provider.serverID + "/Level/" +
                                                  Level.info.name + "/Bases.dat");
                    var structures = allBuildables.OfType<StructureBuildable>().ToList();
                    var barricades = allBuildables.OfType<BarricadeBuildable>().ToList();

                    var buildableCount = river.ReadInt32();

                    if (allBuildables.Count() != buildableCount)
                    {
                        Logging.Write("BaseClustering",
                            "Warning! Buildable count doesn't match saved count! Buildable save data was most likely modified or lost during server downtime. Clusters will be now rebuilt.",
                            ConsoleColor.Yellow);
                        return false;
                    }

                    var clusterCount = river.ReadInt32();
                    var rate = Math.Floor(clusterCount * 0.085);
                    Logging.Write("BaseClustering",
                        $"Loading saved clusters... 0% [0/{clusterCount}] {timer.ElapsedMilliseconds}ms",
                        ConsoleColor.Cyan);

                    for (var i = 0; i < clusterCount; i++)
                    {
                        var builds = new List<Buildable>();
                        // Restore of instanceId is needed to maintain something unique to each cluster cross-restarts.
                        var instanceId = river.ReadInt32();
                        var global = river.ReadBoolean();

                        var buildCount = river.ReadInt32();
                        for (var o = 0; o < buildCount; o++)
                        {
                            var buildInstanceId = river.ReadUInt32();
                            var isStructure = river.ReadBoolean();
                            var build = isStructure
                                ? (Buildable) structures.FirstOrDefault(k => k.InstanceId == buildInstanceId)
                                : barricades.FirstOrDefault(k => k.InstanceId == buildInstanceId);

                            if (build == null)
                            {
                                Logging.Write("BaseClustering",
                                    $"Warning! Buildable with InstanceId {buildInstanceId} [isStructure: {isStructure}] not found! Save data was most likely modified or lost during server downtime. Clusters will be now rebuilt.",
                                    ConsoleColor.Yellow);
                                river.CloseRiver();
                                return false;
                            }

                            builds.Add(build);
                        }

                        bases.Add(new BaseCluster(this, instanceId, global, builds));

                        if ((i + 1) % rate == 0)
                            Logging.Write("BaseClustering",
                                $"Loading saved clusters... {Math.Ceiling((i + 1) / (double) clusterCount * 100)}% [{i + 1}/{clusterCount}] {timer.ElapsedMilliseconds}ms",
                                ConsoleColor.Cyan);
                    }

                    Clusters = new ObservableCollection<BaseCluster>(bases);

                    if (Clusters.Count > 0)
                        m_InstanceCount = Clusters.Max(k => k.InstanceId) + 1;

                    for (var i = 0; i < m_InstanceCount; i++)
                    {
                        if (Clusters.Any(k => k.InstanceId == i))
                            continue;

                        m_FreeInstanceIds.Enqueue(i);
                    }

                    timer.Stop();
                    return true;
                }
                catch (Exception ex)
                {
                    Logging.Write("BaseClustering",
                        $"Warning! An exception was thrown when attempting to load the save file. Assuming the data is corrupted. Clusters will be now rebuilt. Exception: {ex}",
                        ConsoleColor.Yellow);
                    m_InstanceCount = 0;
                    m_FreeInstanceIds = new ConcurrentQueue<int>();
                    return false;
                }
            }
        }

        private static void ForceDataSave()
        {
            SaveManager.save();
        }

        public void Save()
        {
            var river = new RiverExpanded(ServerSavedata.directory + "/" + Provider.serverID + "/Level/" +
                                          Level.info.name + "/Bases.dat");
            river.WriteInt32(PostProcessedBuildables.Count);
            river.WriteInt32(Clusters.Count);
            foreach (var cluster in Clusters)
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
        }

        public int GetBestInstanceId()
        {
            lock (m_InstanceCountLock)
            {
                return !m_FreeInstanceIds.TryDequeue(out var element) ? m_InstanceCount++ : element;
            }
        }

        [NotNull]
        public IReadOnlyCollection<Buildable> PostProcessedBuildables
        {
            get
            {
                lock (Clusters)
                {
                    return Clusters?.SelectMany(k => k.Buildables).Where(k => k.Asset != null).ToList().AsReadOnly();
                }
            }
        }

        /// <summary>
        /// Retrieves all clusters that have the player as the most common owner.
        /// </summary>
        /// <param name="player">The player to use for the search as the most common owner.</param>
        /// <returns>An IEnumerable holding all the clusters that this player is deemed "most common owner" of.</returns>
        [NotNull]
        public IEnumerable<BaseCluster> GetMostOwnedClusters(CSteamID player)
        {
            return GetClustersWithFilter(k => k.CommonOwner == player.m_SteamID);
        }

        /// <summary>
        /// Retrieves all clusters that satisfy the custom filter.
        /// </summary>
        /// <param name="filter">An anonymous function that takes BaseCluster as parameter and returns bool.</param>
        /// <returns>A list of clusters that satisfy the filter.</returns>
        [NotNull]
        public IEnumerable<BaseCluster> GetClustersWithFilter(Func<BaseCluster, bool> filter)
        {
            return Clusters?.Where(filter) ?? new List<BaseCluster>();
        }

        /// <summary>
        /// Gets the cluster that contains the element with the provided model.
        /// </summary>
        /// <param name="model">The model of the buildable within a cluster.</param>
        /// <returns>The cluster in which the model is located within. Null if no cluster is found.</returns>
        [CanBeNull]
        public BaseCluster GetClusterWithElement(Transform model)
        {
            return Clusters?.FirstOrDefault(k => k.Buildables.Any(l => l.Model == model));
        }

        /// <summary>
        /// Gets the cluster that contains the element with the provided position.
        /// </summary>
        /// <param name="instanceId">The instanceId of the buildable within a cluster.</param>
        /// <param name="isStructure">If the instanceId belongs to a structure or a barricade.</param>
        /// <returns>The cluster containing the element provided.</returns>
        [CanBeNull]
        public BaseCluster GetClusterWithElement(uint instanceId, bool isStructure)
        {
            return Clusters?.FirstOrDefault(k =>
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
        /// <returns>The cluster this buildable belongs to.</returns>
        [CanBeNull]
        public BaseCluster GetClusterWithElement(Buildable buildable)
        {
            return Clusters?.FirstOrDefault(k => k.Buildables.Contains(buildable));
        }

        private void BuildableDestroyed(Buildable buildable)
        {
            var cluster = GetClusterWithElement(buildable);
            cluster?.Buildables.Remove(buildable);
        }

        private void BuildableTransformed([NotNull] Buildable buildable)
        {
            BuildableDestroyed(buildable);
            BuildableSpawned(buildable);
        }

        private void BuildableSpawned([NotNull] Buildable buildable)
        {
            // On spawning, check if its a barricade
            if (buildable is BarricadeBuildable)
            {
                // Find the best cluster for this barricade. Only the first result should be considered for barricades.
                var bestCluster = ClusteringTool.FindBestCluster(buildable);

                // If we find a best cluster, add it on it.
                if (bestCluster != null)
                {
                    bestCluster.Buildables.Add(buildable);
                    return;
                }

                // If we don't, re-check for global cluster, and if there's no global cluster, create one.
                var gCluster = Clusters.FirstOrDefault(k => k.IsGlobalCluster);
                if (gCluster != null)
                    gCluster.Buildables.Add(buildable);
                else
                    Clusters.Add(new BaseCluster(this, true, new List<Buildable> {buildable}));

                return;
            }

            // Otherwise, if its a structure, find all the clusters where it'd make a good target, and exclude any global clusters from the result.
            var bestClusters = ClusteringTool.FindBestClusters(buildable).Where(k => !k.IsGlobalCluster).ToList();

            switch (bestClusters.Count)
            {
                // If there's no results, create a new non-global cluster for this new base.
                case 0:
                    Clusters.Add(new BaseCluster(this, false, new List<Buildable> {buildable}));
                    return;
                // If there's exactly 1 cluster found, simply add it to that cluster.
                case 1:
                    bestClusters.First().Buildables.Add(buildable);
                    return;
            }

            // However, if there's more than 1 cluster, select every single buildable from all found clusters
            // Plus include all the buildables from the global cluster.
            var globalCluster = Clusters.FirstOrDefault(k => k.IsGlobalCluster);
            var allBuilds = bestClusters.SelectMany(k => k.Buildables)
                .Concat(globalCluster?.Buildables?.AsEnumerable() ?? Array.Empty<Buildable>()).ToList();

            // And ask the clustering tool to generate new clusters.
            var newClusters = ClusteringTool.ClusterElements(allBuilds);

            // From the result, get all clusters generated that are not global
            var clustersNoGlobal = newClusters.Where(k => !k.IsGlobalCluster).ToList();
            // And all the clusters that are global.
            var clustersGlobal = newClusters.Where(k => k.IsGlobalCluster);

            // For all the found best clusters, we can now un-register them, as they are no longer needed.
            foreach (var c in bestClusters)
                Clusters.Remove(c);

            // New non-global clusters can be safely added now.
            foreach (var c in clustersNoGlobal)
            {
                Clusters.Add(c);
                // Any barricades from the clusters that is a barricade has to be made sure that it isn't in the global cluster anymore.
                foreach (var b in c.Buildables.OfType<BarricadeBuildable>())
                    globalCluster?.Buildables?.Remove(b);
            }

            // For all generated global clusters, we need to either find a better cluster or add it to the existing global cluster.
            foreach (var b in clustersGlobal.SelectMany(k => k.Buildables))
            {
                // Find the next best cluster that this element is within
                var bestCluster = ClusteringTool.FindBestCluster(b);

                // If something is found, add the barricade to it.
                if (bestCluster != null)
                {
                    bestCluster.Buildables.Add(b);
                    continue;
                }

                // If no best cluster is found, check if we have a global cluster. If we do, add the barricade to it. If we don't, create a new global cluster.
                if (globalCluster != null)
                    globalCluster.Buildables?.Add(b);
                else
                    Clusters.Add(new BaseCluster(this, true, new List<Buildable> {b}));
            }
        }

        private void ClustersChanged(object sender, [NotNull] NotifyCollectionChangedEventArgs e)
        {
            lock (m_InstanceCountLock)
            {
                // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        // Raise an event for every single item added.
                        foreach (var cluster in e.NewItems.Cast<BaseCluster>())
                        {
                            Logging.Verbose("New cluster",
                                $"A new cluster (ID: {cluster.InstanceId}) was created & added at {cluster.AverageCenterPosition}. Total buildables: {cluster.Buildables.Count}.");
                            OnClusterAdded?.Invoke(cluster);

                            if (m_InstanceCount <= cluster.InstanceId)
                                m_InstanceCount = cluster.InstanceId + 1;
                        }

                        break;
                    case NotifyCollectionChangedAction.Remove:
                    case NotifyCollectionChangedAction.Replace:
                        // Raise an event for every single item removed.
                        foreach (var cluster in e.OldItems.Cast<BaseCluster>())
                        {
                            Logging.Verbose("ClustersChanged",
                                $"Cluster [{cluster.InstanceId}] {e.Action.ToString().ToLower()}d at index {e.OldStartingIndex}");
                            OnClusterRemoved?.Invoke(cluster);
                            m_FreeInstanceIds.Enqueue(cluster.InstanceId);
                        }

                        break;
                    case NotifyCollectionChangedAction.Reset:
                        Logging.Verbose("ClustersChanged",
                            "Clusters collection was reset. Most likely a Clear() call.");
                        OnClustersCleared?.Invoke();
                        m_FreeInstanceIds = new ConcurrentQueue<int>();
                        m_InstanceCount = 0;
                        break;
                }
            }
        }
    }
}