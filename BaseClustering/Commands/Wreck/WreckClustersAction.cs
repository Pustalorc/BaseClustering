using System.Collections.Generic;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Clusters.Interfaces;
using Rocket.API;
using SDG.Unturned;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.Commands.Wreck;

/// <summary>
///     A wreck action for a collection of <see cref="IBaseCluster" />s.
///     <br />
///     Actions determine how the plugin will do a final search when performing a confirmed wreck.
/// </summary>
internal readonly struct WreckClustersAction
{
    /// <summary>
    ///     The player we might possibly be targeting that owns specific clusters.
    /// </summary>
    public IRocketPlayer? TargetPlayer { get; }

    /// <summary>
    ///     The center position of the wreck action.
    ///     <br />
    ///     If no position is wanted, this value is to be set to <see cref="Vector3.negativeInfinity" />.
    /// </summary>
    public Vector3 Center { get; }

    /// <summary>
    ///     A list of all <see cref="ItemAsset" />s that will be targeted.
    /// </summary>
    public List<ItemAsset> ItemAssets { get; }

    /// <summary>
    ///     The radius based on the Center specified in this object.
    /// </summary>
    public float Radius { get; }

    /// <summary>
    ///     The name of the user input for item asset search, or the name of the only item asset used.
    /// </summary>
    public string ItemAssetName { get; }

    /// <summary>
    ///     Creates a new instance of the class.
    /// </summary>
    /// <param name="target">The player we might possibly be targeting that owns specific clusters.</param>
    /// <param name="center">
    ///     The center position of the wreck action.
    ///     <br />
    ///     If no position is wanted, this value is to be set to <see cref="Vector3.negativeInfinity" />.
    /// </param>
    /// <param name="assets">A list of all <see cref="ItemAsset" />s that will be targeted.</param>
    /// <param name="radius">The radius based on the Center specified in this object.</param>
    /// <param name="assetName">The name of the user input for item asset search, or the name of the only item asset used.</param>
    public WreckClustersAction(IRocketPlayer? target, Vector3 center, List<ItemAsset> assets, float radius,
        string assetName)
    {
        TargetPlayer = target;
        Center = center;
        ItemAssets = assets;
        Radius = radius;
        ItemAssetName = assetName;
    }
}