using JetBrains.Annotations;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Clusters.Interfaces;

namespace Pustalorc.Libraries.BaseClustering.API.BaseClusters.Directory.Events.BaseClusterRemovedEvent;

/// <summary>
///     The arguments for the <see cref="BaseClusterRemovedEvent" />
/// </summary>
/// <param name="cluster">The affected cluster for the event.</param>
[PublicAPI]
public struct BaseClusterRemovedEventArguments(IBaseCluster cluster)
{
    /// <summary>
    ///     The affected cluster for this event.
    /// </summary>
    public IBaseCluster Cluster { get; } = cluster;
}