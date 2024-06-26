extern alias JetBrainsAnnotations;
using JetBrainsAnnotations::JetBrains.Annotations;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Clusters.Interfaces;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Configuration.Interfaces;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Directory.Interfaces;

namespace Pustalorc.Libraries.BaseClustering.API.BaseClusters.Pool.Interfaces;

/// <summary>
///     An interface for the class managing a pool of <see cref="IBaseCluster" />s.
/// </summary>
[PublicAPI]
public interface IBaseClusterPool
{
    /// <summary>
    ///     The next instance id that this pool will assign, should a new <see cref="IBaseCluster" /> be created.
    /// </summary>
    public uint NextInstanceId { get; }

    /// <summary>
    ///     Changes the stored cluster rules for new <see cref="IBaseCluster" />s to the specified one.
    /// </summary>
    /// <param name="clusterRules">The new cluster rules to use for all new <see cref="IBaseCluster" />s.</param>
    /// <remarks>
    ///     This method might not change <see cref="IBaseClusterDirectory" />'s Cluster Rules.
    ///     If you are using this method, please target <see cref="IBaseClusterDirectory" />'s method too.
    /// </remarks>
    public void ChangeClusterRules(IClusterRules clusterRules);

    /// <summary>
    ///     Fills the pool up to the specified limit
    /// </summary>
    public void FillPool(int limit = 25);

    /// <summary>
    ///     Returns and resets an <see cref="IBaseCluster" /> to the pool.
    /// </summary>
    /// <param name="cluster">The <see cref="IBaseCluster" /> to reset and return to the pool.</param>
    public bool Return(IBaseCluster? cluster);

    /// <summary>
    ///     Sets the next instance id for the next <see cref="IBaseCluster" />.
    /// </summary>
    /// <param name="nextInstanceId">The next instance id</param>
    /// <remarks>
    ///     When creating your own pool, this method should prevent the nextInstanceId from being smaller than the
    ///     currently stored nextInstanceId.
    /// </remarks>
    public void SetNextInstanceId(uint nextInstanceId);

    /// <summary>
    ///     Gets a <see cref="IBaseCluster" /> from the pool.
    ///     <br />
    ///     If a <see cref="IBaseCluster" /> isn't available from the pool, a new instance will be created and provided.
    /// </summary>
    /// <param name="isGlobalCluster">
    ///     If the generated <see cref="IBaseCluster" /> should be set to be a global cluster.
    /// </param>
    /// <returns>An instance of type <see cref="IBaseCluster" />.</returns>
    /// <remarks>
    ///     If <paramref name="isGlobalCluster" /> is set to true,
    ///     a new <see cref="IBaseCluster" /> will always be generated.
    /// </remarks>
    public IBaseCluster GetOrCreatePooledCluster(bool isGlobalCluster = false);

    /// <summary>
    ///     Creates a new <see cref="IBaseCluster" />.
    /// </summary>
    /// <param name="instanceId">The instance id to give to the generated <see cref="IBaseCluster" />.</param>
    /// <param name="globalCluster">If the cluster should be created as a global <see cref="IBaseCluster" />.</param>
    /// <returns>A new instance of <see cref="IBaseCluster" />.</returns>
    /// <remarks>
    ///     This method is exposed to skip the pool and assign a specific instanceId to the <see cref="IBaseCluster" />.
    ///     If you use this, you might end up with <see cref="IBaseCluster" />s with duplicate ids.
    /// </remarks>
    public IBaseCluster CreateCluster(uint instanceId, bool globalCluster = false);
}