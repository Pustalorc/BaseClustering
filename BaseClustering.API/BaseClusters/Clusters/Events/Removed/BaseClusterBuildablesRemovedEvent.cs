extern alias JetBrainsAnnotations;
using JetBrainsAnnotations::JetBrains.Annotations;
using Pustalorc.Libraries.RocketModServices.Events.Implementations.Generics;

namespace Pustalorc.Libraries.BaseClustering.API.BaseClusters.Clusters.Events.Removed;

/// <inheritdoc />
/// <summary>
///     An event that is raised when one or more buildables are removed from a cluster.
/// </summary>
[PublicAPI]
public class BaseClusterBuildablesRemovedEvent : Event<BaseClusterBuildablesRemovedEventArguments>;