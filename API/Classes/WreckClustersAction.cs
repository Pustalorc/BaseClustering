using Rocket.API;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.API.Classes
{
    public sealed class WreckClustersAction
    {
        public IRocketPlayer TargetPlayer;
        public Vector3 Center;
        public ItemAsset ItemAsset;
        public float Radius;

        public WreckClustersAction(IRocketPlayer target, Vector3 center, ItemAsset asset, float radius)
        {
            TargetPlayer = target;
            Center = center;
            ItemAsset = asset;
            Radius = radius;
        }
    }
}