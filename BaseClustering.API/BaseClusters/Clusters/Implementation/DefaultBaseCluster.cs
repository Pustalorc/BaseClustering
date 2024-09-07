extern alias JetBrainsAnnotations;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using JetBrainsAnnotations::JetBrains.Annotations;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Clusters.Events.Added;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Clusters.Events.Removed;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Clusters.Events.Reset;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Clusters.Interfaces;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Configuration.Interfaces;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Directory.Interfaces;
using Pustalorc.Libraries.BaseClustering.API.Utilities;
using Pustalorc.Libraries.BuildableAbstractions.API.Buildables.Abstraction;
using Pustalorc.Libraries.BuildableAbstractions.API.Buildables.Implementations;
using Pustalorc.Libraries.RocketModServices.Events.Bus;
using Pustalorc.Libraries.RocketModServices.Services;
using SDG.Unturned;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Pustalorc.Libraries.BaseClustering.API.BaseClusters.Clusters.Implementation;

/// <summary>
///     This class provides a basic default Base Cluster.
/// </summary>
/// <inheritdoc />
[PublicAPI]
public class DefaultBaseCluster : IBaseCluster
{
    /// <inheritdoc />
    public uint InstanceId { get; }

    /// <inheritdoc />
    public virtual ulong Owner => BuildableList.GroupBy(static k => k.Owner).OrderByDescending(static k => k.Count())
        .Select(static g => g.Key).FirstOrDefault();

    /// <inheritdoc />
    public virtual ulong Group => BuildableList.GroupBy(static k => k.Group).OrderByDescending(static k => k.Count())
        .Select(static g => g.Key).FirstOrDefault();

    /// <inheritdoc />
    public virtual Vector3 Center => BuildableList.OfType<StructureBuildable>().AverageCenter(static k => k.Position);

    /// <inheritdoc />
    public virtual IReadOnlyList<Buildable> Buildables => new ReadOnlyCollection<Buildable>(BuildableList);

    /// <summary>
    ///     A modifiable list of all the buildables within this Base Cluster.
    /// </summary>
    protected List<Buildable> BuildableList { get; }

    /// <summary>
    ///     The rules this cluster will uphold.
    /// </summary>
    protected IClusterRules ClusterRules { get; }

    /// <summary>
    ///     Defines if this cluster is being destroyed, and therefore integrity check operations shouldn't be handled.
    /// </summary>
    protected bool IsBeingDestroyed { get; set; }

    /// <summary>
    ///     Constructs a default base cluster.
    /// </summary>
    /// <param name="clusterRules">The rules the cluster will uphold.</param>
    /// <param name="instanceId">This cluster's Instance ID. Do not assign at random, let the Directory handle it.</param>
    public DefaultBaseCluster(IClusterRules clusterRules, uint instanceId)
    {
        ClusterRules = clusterRules;
        BuildableList = [];
        InstanceId = instanceId;
    }

    /// <inheritdoc />
    public virtual bool IsWithinRange(Buildable buildable)
    {
        var structures = Buildables.OfType<StructureBuildable>();
        var distanceCheck = buildable is StructureBuildable
            ? Mathf.Pow(ClusterRules.MaxDistanceBetweenStructures, 2)
            : Mathf.Pow(ClusterRules.MaxDistanceToConsiderPartOfBase, 2);
        return structures.Any(k => (k.Position - buildable.Position).sqrMagnitude <= distanceCheck);
    }

    /// <inheritdoc />
    public virtual bool IsWithinRange(Vector3 vector)
    {
        var distanceCheck = Mathf.Pow(ClusterRules.MaxDistanceToConsiderPartOfBase, 2);

        return Buildables.OfType<StructureBuildable>().Any(k => (k.Position - vector).sqrMagnitude <= distanceCheck);
    }

    /// <inheritdoc />
    public virtual void Reset()
    {
        BuildableList.Clear();
        EventBus.Publish<BaseClusterResetEvent>(new BaseClusterResetEventArguments(this));
    }

    /// <inheritdoc />
    public virtual void AddBuildable(Buildable buildable)
    {
        if (BuildableList.Contains(buildable))
            return;

        BuildableList.Add(buildable);
        var stolenBuildables =
            StealFromCluster(RocketModService<IBaseClusterDirectory>.GetService().GetOrCreateGlobalCluster());
        EventBus.Publish<BaseClusterBuildablesAddedEvent>(
            new BaseClusterBuildablesAddedEventArguments(this, stolenBuildables.Concat([buildable])));
    }

    /// <inheritdoc />
    public virtual void RemoveBuildable(Buildable buildable)
    {
        var removedSomething = BuildableList.Remove(buildable);

        if (!removedSomething)
            return;

        EventBus.Publish<BaseClusterBuildablesRemovedEvent>(
            new BaseClusterBuildablesRemovedEventArguments(this, [buildable]));

        if (IsBeingDestroyed)
            return;

        VerifyAndCorrectIntegrity();
    }

    /// <inheritdoc />
    public virtual void AddBuildables(IEnumerable<Buildable> buildables)
    {
        AddBuildablesInternal(buildables);
    }

    /// <inheritdoc />
    public virtual void RemoveBuildables(IEnumerable<Buildable> buildables)
    {
        var consolidatedBuildables = buildables.ToList();
        var removed = new List<Buildable>();

        foreach (var build in BuildableList.Where(consolidatedBuildables.Remove).ToList())
        {
            BuildableList.Remove(build);
            removed.Add(build);
        }

        if (removed.Count > 0)
            EventBus.Publish<BaseClusterBuildablesRemovedEvent>(
                new BaseClusterBuildablesRemovedEventArguments(this, removed));

        if (removed.Count > 0 && !IsBeingDestroyed)
            VerifyAndCorrectIntegrity();
    }

    /// <inheritdoc />
    public virtual List<Buildable> StealFromCluster(IBaseCluster? cluster)
    {
        if (cluster == null)
            return [];

        var buildablesInRange = cluster.Buildables.Where(IsWithinRange).ToList();
        AddBuildables(buildablesInRange);
        cluster.RemoveBuildables(buildablesInRange);

        return buildablesInRange;
    }

    /// <inheritdoc />
    public virtual void Destroy(bool shouldDropItems = true)
    {
        IsBeingDestroyed = true;

        foreach (var buildable in Buildables.ToList())
        {
            var store = (InteractableStorage?)buildable.Interactable;
            if (store != null)
                store.despawnWhenDestroyed = !shouldDropItems;

            buildable.SafeDestroy();
        }

        RocketModService<IBaseClusterDirectory>.GetService().Unregister(this);
    }

    /// <summary>
    ///     Adds multiple new <see cref="Buildable" />s to this cluster.
    /// </summary>
    /// <param name="buildables">
    ///     An <see cref="IEnumerable{Buildable}" /> with all the new <see cref="Buildable" />s that will
    ///     be added to this cluster.
    /// </param>
    /// <param name="raiseEvent">
    ///     If set to <see langword="false" /> the <see cref="BaseClusterBuildablesAddedEvent" /> will not
    ///     be raised.
    /// </param>
    protected virtual void AddBuildablesInternal(IEnumerable<Buildable> buildables, bool raiseEvent = true)
    {
        var consolidatedBuildables = buildables.Where(buildable => !BuildableList.Contains(buildable)).ToList();
        BuildableList.AddRange(consolidatedBuildables);
        if (raiseEvent && consolidatedBuildables.Count > 0)
            EventBus.Publish<BaseClusterBuildablesAddedEvent>(
                new BaseClusterBuildablesAddedEventArguments(this, consolidatedBuildables));
    }


    /// <summary>
    ///     Verifies that the cluster has structure integrity (all Structures are within range of another Structure)
    /// </summary>
    /// <returns>
    ///     <see langword="true" /> if all Structures are within range of another Structure
    ///     <br />
    ///     <see langword="false" /> otherwise.
    /// </returns>
    protected virtual bool VerifyStructureIntegrity()
    {
        var allStructures = Buildables.OfType<StructureBuildable>().ToList();

        if (allStructures.Count <= 0)
            return false;

        var maxStructureDistance = Mathf.Pow(ClusterRules.MaxDistanceBetweenStructures, 2);
        var succeeded = new List<StructureBuildable>();

        var random = allStructures[Random.Range(0, allStructures.Count)];
        succeeded.Add(random);
        allStructures.Remove(random);

        for (var i = 0; i < succeeded.Count; i++)
        {
            var element = succeeded[i];

            var result = allStructures.Where(k => (element.Position - k.Position).sqrMagnitude <= maxStructureDistance)
                .ToList();
            succeeded.AddRange(result);
            allStructures.RemoveAll(result.Contains);
        }

        return allStructures.Count == 0;
    }

    /// <summary>
    ///     Verifies that the cluster has barricade integrity (all Barricades are within range of a Structure)
    /// </summary>
    /// <returns>
    ///     <see langword="true" /> if all barricades are within range of a Structure
    ///     <br />
    ///     <see langword="false" /> otherwise.
    /// </returns>
    protected virtual bool VerifyBarricadeIntegrity()
    {
        var structures = Buildables.OfType<StructureBuildable>().ToList();

        if (structures.Count <= 0)
            return false;

        var maxBuildableDistance = Mathf.Pow(ClusterRules.MaxDistanceToConsiderPartOfBase, 2);

        return Buildables.OfType<BarricadeBuildable>().All(br =>
            structures.Exists(k => (br.Position - k.Position).sqrMagnitude <= maxBuildableDistance));
    }

    /// <summary>
    ///     This will verify the base integrity (that all the elements are still within range of configured limits) and if not,
    ///     it will correct that.
    /// </summary>
    protected virtual void VerifyAndCorrectIntegrity()
    {
        var directory = RocketModService<IBaseClusterDirectory>.GetService();
        var structureIntegrity = VerifyStructureIntegrity();
        var barricadeIntegrity = VerifyBarricadeIntegrity();

        // If the base is still integrally sound, skip the rest of the code
        if (structureIntegrity && barricadeIntegrity) return;

        var globalCluster = directory.GetOrCreateGlobalCluster();

        IsBeingDestroyed = true;
        // If the structure is still integral, check the barricades and fix any non-integral parts.
        if (structureIntegrity)
        {
            // Get all the barricades that are too far from the cluster in a copied list.
            foreach (var b in Buildables.OfType<BarricadeBuildable>().Where(k => !IsWithinRange(k)).ToList())
            {
                // Find the next best cluster that this element is within
                var bestCluster = directory.FindBestCluster(b);

                // If something is found, check that it's not the same cluster we are already in.
                if (bestCluster != null)
                {
                    if (bestCluster != this)
                    {
                        // If it's a different cluster, remove it from the current cluster and add it to the new one.
                        RemoveBuildable(b);
                        bestCluster.AddBuildable(b);
                    }

                    continue;
                }

                // If no best cluster is found, check if we have a global cluster. If we do, add the barricade to it. If we don't, create a new global cluster.
                RemoveBuildable(b);
                globalCluster.AddBuildable(b);
            }

            IsBeingDestroyed = false;
            return;
        }

        // First, get a list of all buildables to cluster, including global cluster.
        var builds = Buildables.Concat(globalCluster.Buildables).ToList();
        globalCluster.Reset();
        var clusterRegened = directory.ClusterElements(builds)
            .OrderByDescending(static k => k.Buildables.Count).ToList();

        // Dispose correctly of the cluster we are not going to add here.
        var discarded = clusterRegened.FirstOrDefault();
        directory.Unregister(discarded);

        // Select all the clusters, except for the largest one.
        foreach (var c in clusterRegened.Skip(1).ToList())
        {
            // Remove any of the elements on the new cluster from the old one.
            RemoveBuildables(c.Buildables.ToList());

            // Add the new cluster to the directory.
            directory.Register(c);
        }

        // Finally, if there's no structure buildables left in this cluster, call to remove it.
        if (!Buildables.OfType<StructureBuildable>().Any())
            directory.Unregister(this);

        IsBeingDestroyed = false;
    }
}