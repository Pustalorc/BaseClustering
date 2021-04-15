using Rocket.API;
using SDG.Unturned;
using System.Collections.Generic;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.API.WreckingActions
{
    public sealed class WreckClustersAction
    {
        public IRocketPlayer TargetPlayer;
        public Vector3 Center;
        public List<ItemAsset> ItemAssets;
        public float Radius;
        public string ItemAssetName;

        public WreckClustersAction(IRocketPlayer target, Vector3 center, List<ItemAsset> assets, float radius,
            string assetName)
        {
            TargetPlayer = target;
            Center = center;
            ItemAssets = assets;
            Radius = radius;
            ItemAssetName = assetName;
        }
    }
}