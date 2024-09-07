extern alias JetBrainsAnnotations;
using JetBrainsAnnotations::JetBrains.Annotations;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Configuration.Interfaces;

namespace Pustalorc.Libraries.BaseClustering.API.BaseClusters.Configuration.Implementations;

/// <inheritdoc />
/// <summary>
///     The default cluster rules. To be used when no other
///     <see cref="T:Pustalorc.Libraries.BaseClustering.API.BaseClusters.Configuration.Interfaces.IClusterRules" /> is
///     available.
/// </summary>
[PublicAPI]
public class DefaultClusterRules : IClusterRules
{
    /// <inheritdoc />
    public float MaxDistanceBetweenStructures { get; set; } = 6.1f;

    /// <inheritdoc />
    public float MaxDistanceToConsiderPartOfBase { get; set; } = 10f;
}