extern alias JetBrainsAnnotations;
using System.Collections.Generic;
using JetBrainsAnnotations::JetBrains.Annotations;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Clusters.Interfaces;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Configuration.Interfaces;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Pool.Interfaces;
using Pustalorc.Libraries.BuildableAbstractions.API.Buildables.Abstraction;
using UnityEngine;

namespace Pustalorc.Libraries.BaseClustering.API.BaseClusters.Directory.Interfaces;

/// <summary>
///     A base cluster directory, which keeps track and handles all the <see cref="IBaseCluster" />s in the game.
/// </summary>
[PublicAPI]
public interface IBaseClusterDirectory
{
    /// <summary>
    ///     A <see cref="IReadOnlyCollection{IBaseCluster}" /> of all the clusters that this directory is currently keeping
    ///     track of.
    /// </summary>
    public IReadOnlyCollection<IBaseCluster> Clusters { get; }

    /// <summary>
    ///     Changes the stored cluster rules for new <see cref="IBaseCluster" />s to the specified one.
    /// </summary>
    /// <param name="clusterRules">The new cluster rules to use for all new <see cref="IBaseCluster" />s.</param>
    /// <remarks>
    ///     This method should aim to also change <see cref="IBaseClusterPool" />'s Cluster Rules.
    ///     However, it might not, so if you are using this method, please target <see cref="IBaseClusterPool" />'s method too.
    /// </remarks>
    public void ChangeClusterRules(IClusterRules clusterRules);

    /// <summary>
    ///     Registers a new cluster to this directory.
    /// </summary>
    /// <param name="cluster">The cluster that will be registered</param>
    public void Register(IBaseCluster? cluster);

    /// <summary>
    ///     Removes a cluster from this directory, and returns it to the <see cref="IBaseClusterPool" />.
    /// </summary>
    /// <param name="cluster">The cluster that will be removed</param>
    public void Unregister(IBaseCluster? cluster);

    /// <summary>
    ///     Gets or creates a global cluster for the game.
    /// </summary>
    /// <returns>An instance of a class that inherits from <see cref="IBaseCluster" />.</returns>
    public IBaseCluster GetOrCreateGlobalCluster();

    /// <summary>
    ///     Finds the best cluster for the <see cref="Buildable" /> to join.
    /// </summary>
    /// <param name="buildable">The <see cref="Buildable" /> that needs to find a cluster</param>
    /// <returns>An instance of a class that inherits from <see cref="IBaseCluster" />, or null if no cluster was found.</returns>
    public IBaseCluster? FindBestCluster(Buildable buildable);

    /// <summary>
    ///     Finds the best clusters for the <see cref="Buildable" /> to join.
    /// </summary>
    /// <param name="buildable">The <see cref="Buildable" /> that will be used to find the best clusters</param>
    /// <returns>
    ///     An <see cref="IEnumerable{BaseCluster}" /> with the best <see cref="IBaseCluster" />s for the buildable.
    ///     If no best clusters are found, <see cref="IEnumerable{BaseCluster}" /> will be empty.
    /// </returns>
    public IEnumerable<IBaseCluster> FindBestClusters(Buildable buildable);

    /// <summary>
    ///     Finds the best cluster within range of a specific position.
    /// </summary>
    /// <param name="position">The position to check within range.</param>
    /// <returns>
    ///     <see langword="null" /> if no best cluster is available.
    ///     <br />
    ///     An instance of <see cref="IBaseCluster " /> if a best cluster is available.
    /// </returns>
    public IBaseCluster? FindBestCluster(Vector3 position);

    /// <summary>
    ///     Finds the best clusters within range of a specific position.
    /// </summary>
    /// <param name="position">The position to check within range.</param>
    /// <returns>
    ///     An <see cref="IEnumerable{BaseCluster}" /> with the best <see cref="IBaseCluster" />s for the buildable.
    ///     If no best clusters are found, <see cref="IEnumerable{BaseCluster}" /> will be empty.
    /// </returns>
    public IEnumerable<IBaseCluster> FindBestClusters(Vector3 position);

    /// <summary>
    ///     Clusters the specified <see cref="IEnumerable{Buildable}" /> with the current registered ruleset.
    /// </summary>
    /// <param name="buildables">The <see cref="Buildable" />s to cluster.</param>
    /// <param name="shouldLogProgress">If the progress of this clustering should be logged.</param>
    /// <returns>A new <see cref="List{IBaseCluster}" /> with all the generated clusters.</returns>
    public List<IBaseCluster> ClusterElements(IEnumerable<Buildable> buildables, bool shouldLogProgress = false);

    /// <summary>
    ///     Requests that the <see cref="IBaseClusterDirectory" /> regenerates all the clusters.
    /// </summary>
    public void RegenerateClusters();
}