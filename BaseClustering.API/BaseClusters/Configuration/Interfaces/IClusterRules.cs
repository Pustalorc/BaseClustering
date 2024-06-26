extern alias JetBrainsAnnotations;
using JetBrainsAnnotations::JetBrains.Annotations;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Clusters.Interfaces;
using UnityEngine;

namespace Pustalorc.Libraries.BaseClustering.API.BaseClusters.Configuration.Interfaces;

/// <summary>
///     The required rules that clusters will follow.
/// </summary>
[PublicAPI]
public interface IClusterRules
{
    /// <summary>
    ///     The maximum distance between 2 structure objects for them to be considered part of a
    ///     <see cref="Vector3.sqrMagnitude" />.
    ///     <br />
    ///     Distance is in meters by unity's measurements. Squaring is needed if using
    ///     <see cref="Vector3" /> for speed.
    /// </summary>
    public float MaxDistanceBetweenStructures { get; set; }

    /// <summary>
    ///     The maximum distance between a structure object and a position for it to be considered part of, or inside of a
    ///     <see cref="IBaseCluster" />.
    ///     <br />
    ///     Distance is in meters by unity's measurements. Squaring is needed if using
    ///     <see cref="UnityEngine.Vector3.sqrMagnitude" /> for speed.
    /// </summary>
    public float MaxDistanceToConsiderPartOfBase { get; set; }
}