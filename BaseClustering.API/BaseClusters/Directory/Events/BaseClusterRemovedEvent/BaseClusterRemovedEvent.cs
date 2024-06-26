extern alias JetBrainsAnnotations;
using JetBrainsAnnotations::JetBrains.Annotations;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Clusters.Interfaces;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Directory.Interfaces;
using Pustalorc.Libraries.RocketModServices.Events.Implementations.Generics;

namespace Pustalorc.Libraries.BaseClustering.API.BaseClusters.Directory.Events.BaseClusterRemovedEvent;

/// <inheritdoc />
/// <summary>
///     The event that is fired when a <see cref="IBaseCluster" /> is removed/returned from a
///     <see cref="IBaseClusterDirectory" />
/// </summary>
[PublicAPI]
public class BaseClusterRemovedEvent : Event<BaseClusterRemovedEventArguments>;