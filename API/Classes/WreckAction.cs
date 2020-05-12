using Steamworks;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.API.Classes
{
    public sealed class WreckAction
    {
        public CSteamID TargetId;
        public Vector3 Center;
        public ushort ItemId;
        public float Radius;
        public bool IncludeVehicles;
        public bool FilterForBarricades;
        public bool FilterForStructures;

        public WreckAction(bool plants, bool barricades, bool structs, CSteamID target, Vector3 center, ushort id,
            float radius)
        {
            IncludeVehicles = plants;
            FilterForBarricades = barricades;
            FilterForStructures = structs;
            TargetId = target;
            Center = center;
            ItemId = id;
            Radius = radius;
        }
    }
}