extern alias JetBrainsAnnotations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrainsAnnotations::JetBrains.Annotations;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Clusters.Implementation;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Clusters.Interfaces;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Directory.Constants;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Pool.Interfaces;
using Pustalorc.Libraries.BaseClustering.API.FileSystem;
using Pustalorc.Libraries.BuildableAbstractions.API.Buildables.Abstraction;
using Pustalorc.Libraries.BuildableAbstractions.API.Buildables.Implementations;
using Pustalorc.Libraries.BuildableAbstractions.API.Directory.Interfaces;
using Pustalorc.Libraries.BuildableAbstractions.API.Directory.Utils;
using Pustalorc.Libraries.Logging.API.Manager;
using Pustalorc.Libraries.RocketModServices.Services;

namespace Pustalorc.Libraries.BaseClustering.API.BaseClusters.Directory.Utilities;

/// <summary>
///     A utility for Base Cluster Directories to load and save <see cref="IBaseCluster" /> data.
/// </summary>
[PublicAPI]
public class SaveFileLoader
{
    /// <summary>
    ///     The version for the root data of the save file.
    /// </summary>
    protected virtual ushort SaveFileVersion { get; }

    /// <summary>
    ///     The version for the data of clusters in the save file.
    /// </summary>
    protected virtual ushort ClusterDataVersion { get; }

    /// <summary>
    ///     The version for the data of buildables in the save file.
    /// </summary>
    protected virtual ushort BuildableDataVersion { get; }

    /// <summary>
    ///     The path where the save file is located at.
    /// </summary>
    protected string SaveFilePath { get; }

    /// <summary>
    ///     A list of all the clusters that were loaded by this utility
    /// </summary>
    public List<IBaseCluster> LoadedClusters { get; }

    /// <summary>
    ///     Constructs the utility with the bare minimum required.
    /// </summary>
    /// <param name="saveFilePath">The path where the save file is located at.</param>
    public SaveFileLoader(string saveFilePath)
    {
        SaveFileVersion = 1;
        ClusterDataVersion = 1;
        BuildableDataVersion = 1;
        SaveFilePath = saveFilePath;
        LoadedClusters = new List<IBaseCluster>();
        ServiceHelper.GetServiceOrUseDefault();
    }

    /// <summary>
    ///     Saves the specified clusters to the save file.
    /// </summary>
    /// <param name="clusters">The clusters to save to a save file.</param>
    /// <param name="stopwatch">A stopwatch for the logs to know how much time has passed since the start of the operation.</param>
    public virtual void SaveClusters(List<IBaseCluster> clusters, Stopwatch? stopwatch = null)
    {
        stopwatch ??= new Stopwatch();
        if (!stopwatch.IsRunning)
        {
            if (stopwatch.ElapsedMilliseconds > 0)
                stopwatch.Reset();

            stopwatch.Start();
        }

        var buildableDirectory = RocketModService<IBuildableDirectory>.GetService();
        var river = new RiverExpandedUtility(SaveFilePath);
        var clusterCount = clusters.Count;
        var logRate = Math.Floor(clusterCount * 0.085);

        river.WriteUInt16(SaveFileVersion);
        river.WriteInt32(buildableDirectory.BuildableCount);
        river.WriteInt32(clusterCount);

        LogManager.Information(string.Format(LoggingConstants.ClusterSaveProgress, 0, 0, clusterCount,
            stopwatch.ElapsedMilliseconds));
        for (var i = 0; i < clusterCount; i++)
        {
            var position = i + 1;
            SaveBuildablesForCluster(river, clusters[i]);

            if (position % logRate != 0)
                continue;

            var percentageCompleted = Math.Ceiling(position / (double)clusterCount * 100);
            var logMessage = string.Format(LoggingConstants.ClusterSaveProgress, percentageCompleted, position,
                clusterCount, stopwatch.ElapsedMilliseconds);
            LogManager.Information(logMessage);
        }

        LogManager.Information(string.Format(LoggingConstants.ClusterSaveProgress, 100, clusterCount, clusterCount, stopwatch.ElapsedMilliseconds));

        river.CloseRiver();
        stopwatch.Stop();
    }

    /// <summary>
    ///     Saves an <see cref="IBaseCluster" /> and all its <see cref="Buildable" />s.
    /// </summary>
    /// <param name="river">The river utility that is writing this save file.</param>
    /// <param name="cluster">The <see cref="IBaseCluster" /> to save.</param>
    protected virtual void SaveBuildablesForCluster(RiverExpandedUtility river, IBaseCluster cluster)
    {
        river.WriteUInt16(ClusterDataVersion);
        river.WriteUInt32(cluster.InstanceId);
        river.WriteBoolean(cluster is GlobalBaseCluster);
        river.WriteInt32(cluster.Buildables.Count);

        foreach (var buildable in cluster.Buildables)
            SaveBuildable(river, buildable);
    }

    /// <summary>
    ///     Saves a single <see cref="Buildable" />.
    /// </summary>
    /// <param name="river">The river utility that is writing this save file.</param>
    /// <param name="buildable">The <see cref="Buildable" /> to save.</param>
    protected virtual void SaveBuildable(RiverExpandedUtility river, Buildable buildable)
    {
        river.WriteUInt16(BuildableDataVersion);
        river.WriteUInt32(buildable.InstanceId);
        river.WriteBoolean(buildable is StructureBuildable);
    }

    /// <summary>
    ///     Loads all the clusters from the save file.
    /// </summary>
    /// <param name="stopwatch">A stopwatch for the logs to know how much time has passed since the start of the operation.</param>
    /// <returns>
    ///     <see langword="false" /> if a buildable count mismatch happened, and therefore clusters should be rebuilt.
    ///     <br />
    ///     <see langword="true" /> otherwise.
    /// </returns>
    public virtual bool LoadClusters(Stopwatch? stopwatch = null)
    {
        stopwatch ??= new Stopwatch();
        if (!stopwatch.IsRunning)
        {
            if (stopwatch.ElapsedMilliseconds > 0)
                stopwatch.Reset();

            stopwatch.Start();
        }

        LoadedClusters.Clear();
        var buildableDirectory = RocketModService<IBuildableDirectory>.GetService();
        var baseClusterPool = RocketModService<IBaseClusterPool>.GetService();
        var river = new RiverExpandedUtility(SaveFilePath);

        // ReSharper disable once UnusedVariable
        // To be used in the future.
        var saveFileVersion = river.ReadUInt16();

        var buildableCount = river.ReadInt32();

        if (buildableDirectory.BuildableCount != buildableCount)
        {
            LogManager.Debug(string.Format(LoggingConstants.BuildableCountMismatchDebug,
                buildableDirectory.BuildableCount, buildableCount));
            LogManager.Warning(LoggingConstants.BuildableCountMismatch);
            return false;
        }

        var clusterCount = river.ReadInt32();
        var logRate = Math.Floor(clusterCount * 0.085);

        LogManager.Information(string.Format(LoggingConstants.ClusterLoadProgress, 0, 0, clusterCount,
            stopwatch.ElapsedMilliseconds));

        IBaseCluster? globalCluster = null;
        for (var i = 0; i < clusterCount; i++)
        {
            var position = i + 1;
            var success = LoadBuildablesForCluster(river, buildableDirectory, baseClusterPool, ref globalCluster);

            if (!success)
                return false;

            if (position % logRate != 0)
                continue;

            var percentageCompleted = Math.Ceiling(position / (double)clusterCount * 100);
            var logMessage = string.Format(LoggingConstants.ClusterLoadProgress, percentageCompleted, position,
                clusterCount, stopwatch.ElapsedMilliseconds);
            LogManager.Information(logMessage);
        }

        LogManager.Information(string.Format(LoggingConstants.ClusterLoadProgress, 100, clusterCount, clusterCount, stopwatch.ElapsedMilliseconds));

        return true;
    }

    /// <summary>
    ///     Loads all the <see cref="Buildable" />s and all the data for a <see cref="IBaseCluster" />.
    /// </summary>
    /// <param name="river">The river utility that is reading this save file.</param>
    /// <param name="buildableDirectory">The current <see cref="IBuildableDirectory" /> that is being used for this process.</param>
    /// <param name="baseClusterPool">
    ///     The current <see cref="IBaseClusterPool" /> to generate new <see cref="IBaseCluster" />s
    ///     with.
    /// </param>
    /// <param name="globalCluster">The current global cluster.</param>
    /// <returns>
    ///     <see langword="false" /> if a buildable count mismatch happened, and therefore clusters should be rebuilt.
    ///     <br />
    ///     <see langword="true" /> otherwise.
    /// </returns>
    protected virtual bool LoadBuildablesForCluster(RiverExpandedUtility river, IBuildableDirectory buildableDirectory,
        IBaseClusterPool baseClusterPool, ref IBaseCluster? globalCluster)
    {
        var buildables = new List<Buildable>();
        // ReSharper disable once UnusedVariable
        // To be used in the future.
        var clusterDataVersion = river.ReadUInt16();
        var instanceId = river.ReadUInt32();
        var isGlobalCluster = river.ReadBoolean();

        var buildCount = river.ReadInt32();
        for (var o = 0; o < buildCount; o++)
        {
            var build = LoadBuildable(river, buildableDirectory);
            if (build == null)
                return false;

            buildables.Add(build);
        }

        if (isGlobalCluster && globalCluster != null)
        {
            globalCluster.AddBuildables(buildables);
            return true;
        }

        var cluster = baseClusterPool.CreateCluster(instanceId, isGlobalCluster);
        cluster.AddBuildables(buildables);
        LoadedClusters.Add(cluster);

        if (isGlobalCluster)
            globalCluster = cluster;

        return true;
    }

    /// <summary>
    ///     Loads a single <see cref="Buildable" />.
    /// </summary>
    /// <param name="river">The river utility that is reading this save file.</param>
    /// <param name="buildableDirectory">The current <see cref="IBuildableDirectory" /> that is being used for this process.</param>
    /// <returns>A <see cref="Buildable" /> if found, <see langword="null" /> otherwise.</returns>
    protected virtual Buildable? LoadBuildable(RiverExpandedUtility river, IBuildableDirectory buildableDirectory)
    {
        // ReSharper disable once UnusedVariable
        // To be used in the future.
        var buildableDataVersion = river.ReadUInt16();
        var buildInstanceId = river.ReadUInt32();
        var isStructure = river.ReadBoolean();
        var build = isStructure
            ? (Buildable?)buildableDirectory.GetBuildable<StructureBuildable>(buildInstanceId)
            : buildableDirectory.GetBuildable<BarricadeBuildable>(buildInstanceId);

        if (build != null) return build;

        var logMessage = string.Format(LoggingConstants.BuildNotFoundDuringLoad, buildInstanceId, isStructure);
        LogManager.Warning(logMessage);
        river.CloseRiver();

        return build;
    }
}