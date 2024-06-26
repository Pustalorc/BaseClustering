extern alias JetBrainsAnnotations;
using JetBrainsAnnotations::JetBrains.Annotations;
using Pustalorc.Libraries.RocketModServices.Events.Implementations.Generics;

namespace Pustalorc.Libraries.BaseClustering.API.BaseClusters.Directory.Events.BaseClusterAddedEvent;

/// <inheritdoc />
/// <summary>
///     An event that is raised when a new Base Cluster is generated and added.
/// </summary>
[PublicAPI]
public class BaseClusterAddedEvent : Event<BaseClusterAddedEventArguments>;