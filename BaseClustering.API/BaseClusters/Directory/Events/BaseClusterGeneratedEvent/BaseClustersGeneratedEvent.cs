extern alias JetBrainsAnnotations;
using JetBrainsAnnotations::JetBrains.Annotations;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Clusters.Interfaces;
using Pustalorc.Libraries.RocketModServices.Events.Implementations;

namespace Pustalorc.Libraries.BaseClustering.API.BaseClusters.Directory.Events.BaseClusterGeneratedEvent;

/// <inheritdoc />
/// <summary>
///     The event that is fired when all <see cref="IBaseCluster" />s are generated.
/// </summary>
[PublicAPI]
public class BaseClustersGeneratedEvent : Event;