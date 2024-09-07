extern alias JetBrainsAnnotations;
using JetBrainsAnnotations::JetBrains.Annotations;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Clusters.Interfaces;

namespace Pustalorc.Libraries.BaseClustering.API.BaseClusters.Clusters.Events.Reset;

/// <summary>
///     A struct for the arguments that are given to the <see cref="BaseClusterResetEvent" /> callbacks.
/// </summary>
[PublicAPI]
public struct BaseClusterResetEventArguments
{
    /// <summary>
    ///     The cluster that was reset.
    /// </summary>
    public IBaseCluster Cluster { get; }

    /// <summary>
    ///     Constructs the event's params struct
    /// </summary>
    /// <param name="cluster">The cluster that was reset.</param>
    public BaseClusterResetEventArguments(IBaseCluster cluster)
    {
        Cluster = cluster;
    }
}