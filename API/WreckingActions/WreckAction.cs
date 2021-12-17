using Rocket.API;
using SDG.Unturned;
using System.Collections.Generic;
using Pustalorc.Plugins.BaseClustering.API.Buildables;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.API.WreckingActions;

/// <summary>
/// A wreck action for a collection of <see cref="Buildable"/>s.
/// <br/>
/// Actions determine how the plugin will do a final search when performing a confirmed wreck.
/// </summary>
public readonly struct WreckAction
{
    /// <summary>
    /// The player we might possibly be targeting that owns specific clusters.
    /// </summary>
    public IRocketPlayer? TargetPlayer { get; }

    /// <summary>
    /// The center position of the wreck action.
    /// <br/>
    /// If no position is wanted, this value is to be set to <see cref="Vector3.negativeInfinity"/>.
    /// </summary>
    public Vector3 Center { get; }

    /// <summary>
    /// A list of all <see cref="ItemAsset"/>s that will be targeted.
    /// </summary>
    public List<ItemAsset> ItemAssets { get; }

    /// <summary>
    /// The name of the user input for item asset search, or the name of the only item asset used.
    /// </summary>
    public string ItemAssetName { get; }

    /// <summary>
    /// The radius based on the Center specified in this object.
    /// </summary>
    public float Radius { get; }

    /// <summary>
    /// If the action should care about things on vehicles.
    /// </summary>
    public bool IncludeVehicles { get; }

    /// <summary>
    /// If the action should filter only for barricades.
    /// </summary>
    public bool FilterForBarricades { get; }

    /// <summary>
    /// If the action should filter only for structures.
    /// </summary>
    public bool FilterForStructures { get; }

    /// <summary>
    /// Creates a new instance of the class.
    /// </summary>
    /// <param name="plants">If the action should care about things on vehicles.</param>
    /// <param name="barricades">If the action should filter only for barricades.</param>
    /// <param name="structs">If the action should filter only for structures.</param>
    /// <param name="target">The player we might possibly be targeting that owns specific clusters.</param>
    /// <param name="center">
    /// The center position of the wreck action.
    /// <br/>
    /// If no position is wanted, this value is to be set to <see cref="Vector3.negativeInfinity"/>.
    /// </param>
    /// <param name="assets">A list of all <see cref="ItemAsset"/>s that will be targeted.</param>
    /// <param name="radius">The radius based on the Center specified in this object.</param>
    /// <param name="itemAssetName">The name of the user input for item asset search, or the name of the only item asset used.</param>
    public WreckAction(bool plants, bool barricades, bool structs, IRocketPlayer? target, Vector3 center,
        List<ItemAsset> assets, float radius, string itemAssetName)
    {
        IncludeVehicles = plants;
        FilterForBarricades = barricades;
        FilterForStructures = structs;
        TargetPlayer = target;
        Center = center;
        ItemAssets = assets;
        Radius = radius;
        ItemAssetName = itemAssetName;
    }
}