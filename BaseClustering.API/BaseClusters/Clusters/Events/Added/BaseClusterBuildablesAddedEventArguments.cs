extern alias JetBrainsAnnotations;
using System.Collections.Generic;
using System.Linq;
using JetBrainsAnnotations::JetBrains.Annotations;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Clusters.Interfaces;
using Pustalorc.Libraries.BuildableAbstractions.API.Buildables.Abstraction;

namespace Pustalorc.Libraries.BaseClustering.API.BaseClusters.Clusters.Events.Added;

/// <summary>
///     A struct for the arguments that are given to the <see cref="BaseClusterBuildablesAddedEvent" /> callbacks.
/// </summary>
[PublicAPI]
public struct BaseClusterBuildablesAddedEventArguments
{
    /// <summary>
    ///     The cluster that was affected by this change.
    /// </summary>
    public IBaseCluster Cluster { get; }

    /// <summary>
    ///     The buildables that are added to the cluster.
    /// </summary>
    public List<Buildable> Buildables { get; }

    /// <summary>
    ///     Constructs the event's params struct
    /// </summary>
    /// <param name="cluster">The cluster that was affected by this change.</param>
    /// <param name="buildables">The buildables that are added to the cluster.</param>
    public BaseClusterBuildablesAddedEventArguments(IBaseCluster cluster, IEnumerable<Buildable> buildables)
    {
        Cluster = cluster;
        Buildables = buildables.ToList();
    }
}