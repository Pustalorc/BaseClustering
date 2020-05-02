using SDG.Unturned;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.API.Classes
{
    public class Buildable
    {
        // Barricade.cs / Structure.cs
        public ushort AssetId;
        public ushort Health;
        public byte[] State;

        // BarricadeData.cs / StructureData.cs
        public ulong Owner;
        public ulong Group;
        public byte AngleX;
        public byte AngleY;
        public byte AngleZ;
        public Vector3 Position;

        // BarricadeDrop.cs / StructureDrop.cs
        public Transform Model;
        public Interactable Interactable;

        // Multiple Files
        public uint InstanceId;
        public Asset Asset;

        public Buildable(byte angleX, byte angleY, byte angleZ, ushort assetId, ushort health, uint instanceId,
            ulong owner, ulong group,
            Vector3 position, Asset asset, Transform model, Interactable interactable, byte[] state)
        {
            AssetId = assetId;
            Health = health;
            State = state;
            Owner = owner;
            Group = group;
            AngleX = angleX;
            AngleY = angleY;
            AngleZ = angleZ;
            Position = position;
            Model = model;
            Interactable = interactable;
            InstanceId = instanceId;
            Asset = asset;
        }
    }
}