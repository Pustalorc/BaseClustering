using Rocket.API;
using SDG.Unturned;
using System.Collections.Generic;
using Pustalorc.Plugins.BaseClustering.API.BaseClusters;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.API.WreckingActions
{
    /// <summary>
    /// A wreck action for a <see cref="BaseCluster"/>.
    /// <br/>
    /// Actions determine how the plugin will do a final search when performing a confirmed wreck.
    /// </summary>
    public sealed class WreckClustersAction
    {
        /// <summary>
        /// The player we might possibly be targeting that owns specific clusters.
        /// </summary>
        public IRocketPlayer? TargetPlayer;

        /// <summary>
        /// The center position of the wreck action.
        /// <br/>
        /// If no position is wanted, this value is to be set to <see cref="Vector3.negativeInfinity"/>.
        /// </summary>
        public Vector3 Center;

        /// <summary>
        /// A list of all <see cref="ItemAsset"/>s that will be targeted.
        /// </summary>
        public List<ItemAsset> ItemAssets;

        /// <summary>
        /// The radius based on the Center specified in this object.
        /// </summary>
        public float Radius;

        /// <summary>
        /// The name of the user input for item asset search, or the name of the only item asset used.
        /// </summary>
        public string ItemAssetName;

        public WreckClustersAction(IRocketPlayer? target, Vector3 center, List<ItemAsset> assets, float radius, string assetName)
        {
            TargetPlayer = target;
            Center = center;
            ItemAssets = assets;
            Radius = radius;
            ItemAssetName = assetName;
        }
    }
}