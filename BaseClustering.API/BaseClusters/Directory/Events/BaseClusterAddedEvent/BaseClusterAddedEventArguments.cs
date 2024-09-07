extern alias JetBrainsAnnotations;
using JetBrainsAnnotations::JetBrains.Annotations;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Clusters.Interfaces;

namespace Pustalorc.Libraries.BaseClustering.API.BaseClusters.Directory.Events.BaseClusterAddedEvent;

/// <summary>
/// </summary>
[PublicAPI]
public struct BaseClusterAddedEventArguments
{
    /// <summary>
    /// </summary>
    public IBaseCluster Cluster { get; }

    /// <summary>
    /// </summary>
    /// <param name="cluster"></param>
    public BaseClusterAddedEventArguments(IBaseCluster cluster)
    {
        Cluster = cluster;
    }
}