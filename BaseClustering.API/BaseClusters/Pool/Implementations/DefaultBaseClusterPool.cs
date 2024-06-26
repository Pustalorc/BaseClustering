extern alias JetBrainsAnnotations;
using System.Collections.Concurrent;
using JetBrainsAnnotations::JetBrains.Annotations;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Clusters.Implementation;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Clusters.Interfaces;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Configuration.Implementations;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Configuration.Interfaces;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Directory.Interfaces;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Pool.Interfaces;
using Pustalorc.Libraries.RocketModServices.Services;

namespace Pustalorc.Libraries.BaseClustering.API.BaseClusters.Pool.Implementations;

/// <inheritdoc />
[PublicAPI]
public class DefaultBaseClusterPool : IBaseClusterPool
{
    /// <inheritdoc />
    public uint NextInstanceId { get; private set; }

    private ConcurrentBag<IBaseCluster> ClusterPool { get; }
    private IClusterRules ClusterRules { get; set; }

    /// <summary>
    ///     Instantiates the default base cluster pool.
    /// </summary>
    public DefaultBaseClusterPool()
    {
        ClusterPool = new ConcurrentBag<IBaseCluster>();
        ClusterRules = new DefaultClusterRules();
        NextInstanceId = 0;
    }

    /// <inheritdoc />
    public void ChangeClusterRules(IClusterRules clusterRules)
    {
        if (ClusterRules == clusterRules)
            return;

        RocketModService<IBaseClusterDirectory>.TryGetService()?.ChangeClusterRules(clusterRules);
        ClusterRules = clusterRules;
    }

    /// <inheritdoc />
    public void FillPool(int limit = 25)
    {
        while (ClusterPool.Count < limit)
            ClusterPool.Add(new DefaultBaseCluster(ClusterRules, NextInstanceId++));
    }

    /// <inheritdoc />
    public bool Return(IBaseCluster? cluster)
    {
        if (cluster == null)
            return false;

        cluster.Reset();

        if (cluster is GlobalBaseCluster)
            return false;

        ClusterPool.Add(cluster);
        return true;
    }

    /// <inheritdoc />
    public void SetNextInstanceId(uint nextInstanceId)
    {
        NextInstanceId = nextInstanceId;
    }

    /// <inheritdoc />
    public IBaseCluster GetOrCreatePooledCluster(bool isGlobalCluster = false)
    {
        if (isGlobalCluster)
            return CreateCluster(NextInstanceId++, true);

        return ClusterPool.TryTake(out var baseCluster) ? baseCluster : CreateCluster(NextInstanceId++);
    }

    /// <inheritdoc />
    public IBaseCluster CreateCluster(uint instanceId, bool globalCluster = false)
    {
        return globalCluster
            ? new GlobalBaseCluster(ClusterRules, instanceId)
            : new DefaultBaseCluster(ClusterRules, instanceId);
    }
}