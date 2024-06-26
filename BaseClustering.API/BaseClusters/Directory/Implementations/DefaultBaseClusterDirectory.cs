using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Clusters.Implementation;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Clusters.Interfaces;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Configuration.Implementations;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Configuration.Interfaces;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Directory.Constants;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Directory.Events.BaseClusterAddedEvent;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Directory.Events.BaseClusterGeneratedEvent;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Directory.Events.BaseClusterRemovedEvent;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Directory.Interfaces;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Directory.Utilities;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Pool.Interfaces;
using Pustalorc.Libraries.BuildableAbstractions.API.BuildableChangeDelayer.Events.Destroy;
using Pustalorc.Libraries.BuildableAbstractions.API.BuildableChangeDelayer.Events.Spawn;
using Pustalorc.Libraries.BuildableAbstractions.API.BuildableChangeDelayer.Events.Transform;
using Pustalorc.Libraries.BuildableAbstractions.API.Buildables.Abstraction;
using Pustalorc.Libraries.BuildableAbstractions.API.Buildables.Implementations;
using Pustalorc.Libraries.BuildableAbstractions.API.Directory.Interfaces;
using Pustalorc.Libraries.BuildableAbstractions.API.Directory.Utils;
using Pustalorc.Libraries.Logging.API.Loggers.Configuration.Interfaces;
using Pustalorc.Libraries.Logging.API.LogLevels.Implementations;
using Pustalorc.Libraries.Logging.API.Manager;
using Pustalorc.Libraries.RocketModServices.Events.Bus;
using Pustalorc.Libraries.RocketModServices.Services;
using Pustalorc.Libraries.RocketModServices.Services.Interfaces;
using SDG.Unturned;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Pustalorc.Libraries.BaseClustering.API.BaseClusters.Directory.Implementations;

/// <summary>
///     The default directory that keeps track of all <see cref="IBaseCluster" />s.
/// </summary>
/// <inheritdoc cref="Pustalorc.Libraries.BaseClustering.API.BaseClusters.Directory.Interfaces.IBaseClusterDirectory" />
[PublicAPI]
public class DefaultBaseClusterDirectory : IBaseClusterDirectory, IService
{
    /// <inheritdoc />
    public IReadOnlyCollection<IBaseCluster> Clusters => new ReadOnlyCollection<IBaseCluster>(LoadedClusters);

    private List<IBaseCluster> LoadedClusters { get; set; }
    private IBaseCluster? GlobalCluster { get; set; }
    private IClusterRules ClusterRules { get; set; }
    private SaveFileLoader SaveFileLoader { get; }

    private class LogConfiguration : ILoggerConfiguration
    {
        public byte MaxLogLevel => LogLevel.Debug.Level;
    }

    /// <summary>
    ///     Instantiates the default base cluster directory with default configuration.
    /// </summary>
    public DefaultBaseClusterDirectory()
    {
        LoadedClusters = new List<IBaseCluster>();
        GlobalCluster = null;
        ClusterRules = new DefaultClusterRules();
        SaveFileLoader = new SaveFileLoader(Path.Combine(ServerSavedata.directory, Provider.serverID,
            SaveConstants.LevelFolderName, Level.info.name, SaveConstants.SaveFileName));
        ServiceHelper.GetServiceOrUseDefault();
        LogManager.UpdateConfiguration(new LogConfiguration());
    }

    /// <inheritdoc />
    public void ChangeClusterRules(IClusterRules clusterRules)
    {
        if (ClusterRules == clusterRules)
            return;

        RocketModService<IBaseClusterPool>.TryGetService()?.ChangeClusterRules(clusterRules);
        ClusterRules = clusterRules;
    }

    /// <inheritdoc />
    public void Register(IBaseCluster? cluster)
    {
        if (cluster == null)
            return;

        LoadedClusters.Add(cluster);
        EventBus.Publish<BaseClusterAddedEvent>(new BaseClusterAddedEventArguments(cluster));
    }

    /// <inheritdoc />
    public void Unregister(IBaseCluster? cluster)
    {
        if (cluster == null)
            return;

        var clusterPool = RocketModService<IBaseClusterPool>.TryGetService();

        if (clusterPool == null)
            return;

        var removedSomething = LoadedClusters.Remove(cluster);
        var returnedSomething = clusterPool.Return(cluster);

        if (removedSomething && returnedSomething)
            EventBus.Publish<BaseClusterRemovedEvent>(new BaseClusterRemovedEventArguments(cluster));
    }

    /// <inheritdoc />
    public IBaseCluster GetOrCreateGlobalCluster()
    {
        return GlobalCluster ??= RocketModService<IBaseClusterPool>.GetService().GetOrCreatePooledCluster(true);
    }

    /// <inheritdoc />
    public IBaseCluster? FindBestCluster(Buildable target)
    {
        return FindBestClusters(target).FirstOrDefault();
    }

    /// <inheritdoc />
    public IEnumerable<IBaseCluster> FindBestClusters(Buildable target)
    {
        return Clusters.Where(k => k.IsWithinRange(target))
            .OrderBy(k => (k.Center - target.Position).sqrMagnitude);
    }

    /// <inheritdoc />
    public IBaseCluster? FindBestCluster(Vector3 target)
    {
        return FindBestClusters(target).FirstOrDefault();
    }

    /// <inheritdoc />
    public IEnumerable<IBaseCluster> FindBestClusters(Vector3 target)
    {
        return Clusters.Where(k => k.IsWithinRange(target))
            .OrderBy(k => (k.Center - target).sqrMagnitude);
    }

    // This method has been heavily documented as to not lose what the fuck its meant to do/is doing.
    // It's a pain to read anyway, and definitely could be improved. Feel free to make this better... Please.
    /// <inheritdoc />
    public List<IBaseCluster> ClusterElements(IEnumerable<Buildable> buildables, bool shouldLogProgress = false)
    {
        // Start a new stopwatch. This will be used to log how long the program is taking with each step.
        var stopwatch = Stopwatch.StartNew();

        // Initialize an empty list for the output of this method.
        var output = new List<IBaseCluster>();

        // Retrieve the cluster pool service
        var baseClusterPool = RocketModService<IBaseClusterPool>.GetService();

        // Set constants of squared distance. This will be used on distance checks.
        // Faster than taking the square root of a number.
        var maxStructureDistance = Mathf.Pow(ClusterRules.MaxDistanceBetweenStructures, 2);
        var maxBarricadeDistance = Mathf.Pow(ClusterRules.MaxDistanceToConsiderPartOfBase, 2);

        // Set a couple variables that are used for logging.
        var currentMultiplier = 0;
        var currentCount = 0;

        // Get all the buildables to cluster. Anything planted (ie: it's on a vehicle) should NOT be clustered.
        var buildablesToCluster = buildables.Where(static k => !k.IsPlanted).ToList();

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
            var structuresOfCluster = new List<Buildable>();
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
            var cluster = baseClusterPool.GetOrCreatePooledCluster();
            // Add all the combined buildables to this cluster.
            cluster.AddBuildables(buildablesOfCluster);
            // Add this cluster to the output list.
            output.Add(cluster);

            // Finally, check if we need logging, and if we are ready to log it.
            currentCount += cluster.Buildables.Count;
            if (!shouldLogProgress || !(currentCount / logRate > currentMultiplier)) continue;

            currentMultiplier++;
            var currentProgress = Math.Ceiling(currentCount / (double)totalBuildablesToCluster * 100);
            LogManager.Information(string.Format(LoggingConstants.ClusterGenerationProgress, currentProgress,
                currentCount, totalBuildablesToCluster, stopwatch.ElapsedMilliseconds));
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
        if (shouldLogProgress)
        {
            var finalBuildCount = output.Sum(static k => k.Buildables.Count) + remainingBarricadeCount;
            var currentProgress = Math.Ceiling(finalBuildCount / (double)totalBuildablesToCluster * 100);
            LogManager.Information(string.Format(LoggingConstants.ClusterGenerationProgress, currentProgress,
                finalBuildCount, totalBuildablesToCluster, stopwatch.ElapsedMilliseconds));
        }

        return output;
    }

    /// <inheritdoc />
    public void RegenerateClusters()
    {
        GenerateAndLoadAllClusters(false);
    }

    /// <inheritdoc />
    public void Load()
    {
        if (Level.isLoaded)
            LevelLoaded(0);
        else
            Level.onPostLevelLoaded += LevelLoaded;
    }

    /// <inheritdoc />
    public void Unload()
    {
        SaveManager.onPostSave -= Save;
        EventBus.Unsubscribe<DelayedBuildablesTransformedEvent>((object)BuildablesTransformed);
        EventBus.Unsubscribe<DelayedBuildablesDestroyedEvent>((object)BuildablesDestroyed);
        EventBus.Unsubscribe<DelayedBuildablesSpawnedEvent>((object)BuildablesSpawned);

        Save();

        foreach (var cluster in LoadedClusters.ToList())
            Unregister(cluster);
    }

    /// <summary>
    ///     The method that executes once the full level has been loaded.
    /// </summary>
    protected virtual void LevelLoaded(int id)
    {
        EventBus.Subscribe<DelayedBuildablesSpawnedEvent>((object)BuildablesSpawned);
        EventBus.Subscribe<DelayedBuildablesDestroyedEvent>((object)BuildablesDestroyed);
        EventBus.Subscribe<DelayedBuildablesTransformedEvent>((object)BuildablesTransformed);
        SaveManager.onPostSave += Save;

        GenerateAndLoadAllClusters();
        RocketModService<IBaseClusterPool>.GetService().FillPool();
    }

    private void GenerateAndLoadAllClusters(bool loadSaveFile = true)
    {
        var stopwatch = Stopwatch.StartNew();

        var buildableDirectory = RocketModService<IBuildableDirectory>.GetService();
        LogManager.Information(string.Format(LoggingConstants.BuildablesLoaded, buildableDirectory.BuildableCount,
            stopwatch.ElapsedMilliseconds));

        var successfulLoad = false;
        if (loadSaveFile && LevelSavedata.fileExists($"/{SaveConstants.SaveFileName}"))
            successfulLoad = LoadClusters(buildableDirectory);

        if (!successfulLoad)
        {
            LogManager.Warning(LoggingConstants.ClustersGeneratingWarning);
            LoadedClusters.AddRange(ClusterElements(buildableDirectory.GetBuildables<Buildable>(), true));
        }

        stopwatch.Stop();
        LogManager.Information(string.Format(LoggingConstants.ClustersLoaded, Clusters.Count,
            stopwatch.ElapsedMilliseconds));

        EventBus.Publish<BaseClustersGeneratedEvent>();
    }

    private void Save()
    {
        SaveFileLoader.SaveClusters(LoadedClusters);
    }

    private bool LoadClusters(IBuildableDirectory buildableDirectory)
    {
        foreach (var cluster in LoadedClusters.ToList())
            Unregister(cluster);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (!SaveFileLoader.LoadClusters(stopwatch))
                return false;

            LoadedClusters.AddRange(SaveFileLoader.LoadedClusters);
            GlobalCluster = LoadedClusters.OfType<GlobalBaseCluster>().FirstOrDefault();

            if (Clusters.Count > 0)
            {
                var nextInstanceId = Clusters.Max(static k => k.InstanceId) + 1;
                RocketModService<IBaseClusterPool>.GetService().SetNextInstanceId(nextInstanceId);
            }

            stopwatch.Stop();
            return true;
        }
        catch (Exception exception)
        {
            LogManager.Warning(string.Format(LoggingConstants.ClusterLoadException, exception));

            foreach (var cluster in SaveFileLoader.LoadedClusters)
                Unregister(cluster);

            return false;
        }
    }

    private void BuildablesSpawned(DelayedBuildablesSpawnedEventArguments arguments)
    {
        BuildablesSpawnedInternal(arguments.Buildables);
    }

    private void BuildablesSpawnedInternal(List<Buildable> buildables)
    {
        var baseClusterPool = RocketModService<IBaseClusterPool>.GetService();
        var globalCluster = GetOrCreateGlobalCluster();

        foreach (var buildable in buildables)
        {
            // Planted buildables (on vehicles) will not currently be clustered, as they can move around the map without triggering events.
            if (buildable.IsPlanted) return;

            // On spawning, check if it's a barricade
            if (buildable is BarricadeBuildable)
            {
                // Find the best cluster for this barricade.
                var bestCluster = FindBestCluster(buildable);

                // If we find a best cluster, add it to it.
                if (bestCluster != null)
                {
                    bestCluster.AddBuildable(buildable);
                    return;
                }

                // If we don't, add it to the global cluster.
                globalCluster.AddBuildable(buildable);
                return;
            }

            // Otherwise, if it's a structure, find all the clusters where it'd make a good target, and exclude any global clusters from the result.
            var bestClusters = FindBestClusters(buildable).ToList();

            switch (bestClusters.Count)
            {
                // If there's no results, create a new non-global cluster for this new base.
                case 0:
                    var cluster = baseClusterPool.GetOrCreatePooledCluster();
                    cluster.AddBuildable(buildable);
                    Register(cluster);
                    cluster.StealFromCluster(globalCluster);
                    return;
                // If there's exactly 1 cluster found, simply add it to that cluster.
                case 1:
                    cluster = bestClusters.First();
                    cluster.AddBuildable(buildable);
                    cluster.StealFromCluster(globalCluster);
                    return;

                // However, if there's more than 1 cluster, select every single buildable from all found clusters.
                default:
                    var allBuilds = bestClusters.SelectMany(static k => k.Buildables).ToList();

                    // Make sure to include the buildable we spawned in that set.
                    allBuilds.Add(buildable);

                    // For all the found best clusters, we can now un-register them, as they are no longer needed.
                    foreach (var baseCluster in bestClusters)
                        Unregister(baseCluster);

                    // And ask the clustering tool to generate new clusters, and populate the global cluster.
                    var newClusters = ClusterElements(allBuilds);

                    // New clusters can be safely added now.
                    foreach (var baseCluster in newClusters)
                    {
                        Register(baseCluster);
                        baseCluster.StealFromCluster(baseCluster);
                    }

                    return;
            }
        }
    }

    private void BuildablesDestroyed(DelayedBuildablesDestroyedEventArguments arguments)
    {
        BuildablesDestroyedInternal(arguments.Buildables);
    }

    internal void BuildablesDestroyedInternal(List<Buildable> buildables)
    {
        foreach (var cluster in Clusters.ToList())
        {
            if (buildables.Count == 0)
                return;

            cluster.RemoveBuildables(buildables);
        }
    }

    private void BuildablesTransformed(DelayedBuildablesTransformedEventArguments arguments)
    {
        var buildables = arguments.Buildables;
        BuildablesDestroyedInternal(buildables);
        BuildablesSpawnedInternal(buildables);
    }
}