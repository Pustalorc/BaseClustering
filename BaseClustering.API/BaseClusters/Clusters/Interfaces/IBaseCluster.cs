using System.Collections.Generic;
using JetBrains.Annotations;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Clusters.Events.Reset;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Configuration.Interfaces;
using Pustalorc.Libraries.BuildableAbstractions.API.Buildables.Abstraction;
using UnityEngine;

namespace Pustalorc.Libraries.BaseClustering.API.BaseClusters.Clusters.Interfaces;

/// <summary>
///     An interface definition of what a Base Cluster should hold and provide.
/// </summary>
[PublicAPI]
public interface IBaseCluster
{
    /// <summary>
    ///     An Instance ID to uniquely identify this cluster from others.
    /// </summary>
    public uint InstanceId { get; }

    /// <summary>
    ///     Provides the SteamId of the player that has placed the most elements for this base.
    /// </summary>
    public ulong Owner { get; }

    /// <summary>
    ///     Provides the SteamId of the group that has placed the most elements for this base.
    /// </summary>
    public ulong Group { get; }

    /// <summary>
    ///     The center point of this cluster.
    /// </summary>
    public Vector3 Center { get; }

    /// <summary>
    ///     A read only list of all the buildables in this cluster.
    /// </summary>
    public IReadOnlyList<Buildable> Buildables { get; }

    /// <summary>
    ///     Destroys the cluster and all its buildables.
    /// </summary>
    /// <param name="shouldDropItems">
    ///     If any buildables that can store items should drop their items on destruction, or if they
    ///     should delete the items.
    /// </param>
    public void Destroy(bool shouldDropItems = true);

    /// <summary>
    ///     Resets a cluster to its default state (no buildables).
    ///     This method should raise <see cref="BaseClusterResetEvent" />.
    /// </summary>
    public void Reset();

    /// <summary>
    ///     Checks if a <see cref="Buildable" /> is within range of this cluster.
    /// </summary>
    /// <param name="buildable">The <see cref="Buildable" /> to check.</param>
    /// <returns>
    ///     <see langword="true" /> if the <see cref="Buildable" /> is within range.
    ///     <br />
    ///     <see langword="false" /> if the <see cref="Buildable" /> is outside range.
    /// </returns>
    public bool IsWithinRange(Buildable buildable);

    /// <summary>
    ///     Checks if a <see cref="Vector3" /> falls within range of this cluster.
    /// </summary>
    /// <param name="vector">The <see cref="Vector3" /> to check.</param>
    /// <returns>
    ///     <see langword="true" /> if the <see cref="Vector3" /> is within range.
    ///     <br />
    ///     <see langword="false" /> if the <see cref="Vector3" /> is outside range.
    /// </returns>
    /// <remarks>
    ///     Unlike <see cref="IsWithinRange(Buildable)" />, this method should only check with
    ///     <see cref="IClusterRules.MaxDistanceToConsiderPartOfBase" />. It should not check with
    ///     <see cref="IClusterRules.MaxDistanceBetweenStructures" />.
    /// </remarks>
    public bool IsWithinRange(Vector3 vector);

    /// <summary>
    ///     Adds a new <see cref="Buildable" /> to this cluster.
    /// </summary>
    /// <param name="buildable">The <see cref="Buildable" /> that will be added to the cluster.</param>
    public void AddBuildable(Buildable buildable);

    /// <summary>
    ///     Removes a <see cref="Buildable" /> from this cluster.
    /// </summary>
    /// <param name="buildable">The <see cref="Buildable" /> that will be removed from the cluster.</param>
    public void RemoveBuildable(Buildable buildable);

    /// <summary>
    ///     Adds multiple new <see cref="Buildable" />s to this cluster.
    /// </summary>
    /// <param name="buildables">
    ///     An <see cref="IEnumerable{Buildable}" /> with all the new <see cref="Buildable" />s that will
    ///     be added to this cluster.
    /// </param>
    public void AddBuildables(IEnumerable<Buildable> buildables);

    /// <summary>
    ///     Removes multiple <see cref="Buildable" />s from this cluster.
    /// </summary>
    /// <param name="buildables">
    ///     An <see cref="IEnumerable{Buildable}" /> with all the <see cref="Buildable" />s that will be
    ///     removed from this cluster.
    /// </param>
    public void RemoveBuildables(IEnumerable<Buildable> buildables);

    /// <summary>
    ///     Makes this cluster steal any in-range buildables from the opposing cluster.
    /// </summary>
    /// <param name="cluster">The target cluster to steal buildables from.</param>
    /// <returns>
    ///     A <see cref="List{Buildable}" /> with all of the <see cref="Buildable" />s that were stolen from the target
    ///     cluster.
    /// </returns>
    public List<Buildable> StealFromCluster(IBaseCluster? cluster);
}