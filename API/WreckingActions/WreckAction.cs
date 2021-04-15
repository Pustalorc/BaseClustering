using Rocket.API;
using SDG.Unturned;
using System.Collections.Generic;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.API.WreckingActions
{
    public sealed class WreckAction
    {
        public IRocketPlayer TargetPlayer;
        public Vector3 Center;
        public List<ItemAsset> ItemAssets;
        public string ItemAssetName;
        public float Radius;
        public bool IncludeVehicles;
        public bool FilterForBarricades;
        public bool FilterForStructures;

        public WreckAction(bool plants, bool barricades, bool structs, IRocketPlayer target, Vector3 center,
            List<ItemAsset> asset, float radius, string itemAssetName)
        {
            IncludeVehicles = plants;
            FilterForBarricades = barricades;
            FilterForStructures = structs;
            TargetPlayer = target;
            Center = center;
            ItemAssets = asset;
            Radius = radius;
            ItemAssetName = itemAssetName;
        }
    }
}