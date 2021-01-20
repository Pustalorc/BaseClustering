using Rocket.API;
using SDG.Unturned;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.API.WreckingActions
{
    public sealed class WreckAction
    {
        public IRocketPlayer TargetPlayer;
        public Vector3 Center;
        public ItemAsset ItemAsset;
        public float Radius;
        public bool IncludeVehicles;
        public bool FilterForBarricades;
        public bool FilterForStructures;

        public WreckAction(bool plants, bool barricades, bool structs, IRocketPlayer target, Vector3 center,
            ItemAsset asset,
            float radius)
        {
            IncludeVehicles = plants;
            FilterForBarricades = barricades;
            FilterForStructures = structs;
            TargetPlayer = target;
            Center = center;
            ItemAsset = asset;
            Radius = radius;
        }
    }
}