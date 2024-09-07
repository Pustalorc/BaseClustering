extern alias JetBrainsAnnotations;
using JetBrainsAnnotations::JetBrains.Annotations;
using Pustalorc.Libraries.RocketModServices.Events.Implementations.Generics;

namespace Pustalorc.Libraries.BaseClustering.API.BaseClusters.Clusters.Events.Added;

/// <inheritdoc />
/// <summary>
///     An event that is raised when one or more new buildables are added to a cluster.
/// </summary>
[PublicAPI]
public class BaseClusterBuildablesAddedEvent : Event<BaseClusterBuildablesAddedEventArguments>;