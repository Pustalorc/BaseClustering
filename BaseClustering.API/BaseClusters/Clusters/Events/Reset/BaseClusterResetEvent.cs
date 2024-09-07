extern alias JetBrainsAnnotations;
using JetBrainsAnnotations::JetBrains.Annotations;
using Pustalorc.Libraries.RocketModServices.Events.Implementations.Generics;

namespace Pustalorc.Libraries.BaseClustering.API.BaseClusters.Clusters.Events.Reset;

/// <inheritdoc />
/// <summary>
///     An event that is raised when a cluster is reset.
/// </summary>
[PublicAPI]
public class BaseClusterResetEvent : Event<BaseClusterResetEventArguments>;