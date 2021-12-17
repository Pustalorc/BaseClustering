using Pustalorc.Plugins.BaseClustering.API.BaseClusters;

namespace Pustalorc.Plugins.BaseClustering.API.Delegates;

/// <summary>
/// A delegate that handles any notification about a cluster changing (being added, removed, reset, etc).
/// </summary>
/// <param name="cluster">The affected <see cref="BaseCluster"/>.</param>
public delegate void ClusterChange(BaseCluster cluster);