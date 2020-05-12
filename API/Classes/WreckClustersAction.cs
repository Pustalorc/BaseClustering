using Steamworks;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.API.Classes
{
    public sealed class WreckClustersAction
    {
        public CSteamID TargetId;
        public Vector3 Center;
        public ushort ItemId;
        public float Radius;

        public WreckClustersAction(CSteamID target, Vector3 center, ushort id, float radius)
        {
            TargetId = target;
            Center = center;
            ItemId = id;
            Radius = radius;
        }
    }
}