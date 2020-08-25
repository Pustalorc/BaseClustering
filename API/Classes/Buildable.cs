using SDG.Unturned;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.API.Classes
{
    // This references the original objects. Changes done to them should be reflected in the property accessors here.
    public class Buildable
    {
        private BarricadeData barricadeData;
        private BarricadeDrop barricadeDrop;
        private StructureData structureData;
        private StructureDrop structureDrop;
        private bool IsBarricade;

        // Barricade.cs / Structure.cs
        public ushort AssetId => Asset.id;
        public ushort Health => IsBarricade ? barricadeData.barricade.health : structureData.structure.health;
        public byte[] State => IsBarricade ? barricadeData.barricade.state : null;

        // BarricadeData.cs / StructureData.cs
        public ulong Owner => IsBarricade ? barricadeData.owner : structureData.owner;
        public ulong Group => IsBarricade ? barricadeData.group : structureData.group;
        public byte AngleX => IsBarricade ? barricadeData.angle_x : structureData.angle_x;
        public byte AngleY => IsBarricade ? barricadeData.angle_y : structureData.angle_y;
        public byte AngleZ => IsBarricade ? barricadeData.angle_z : structureData.angle_z;
        public Vector3 Position => IsBarricade ? barricadeData.point : structureData.point;

        // BarricadeDrop.cs / StructureDrop.cs
        public Transform Model => IsBarricade ? barricadeDrop.model : structureDrop.model;
        public Interactable Interactable => IsBarricade ? barricadeDrop.interactable : null;

        // Multiple Files
        public uint InstanceId => IsBarricade ? barricadeData.instanceID : structureData.instanceID;
        public Asset Asset => IsBarricade ? (Asset)barricadeData.barricade.asset : structureData.structure.asset;

        public Buildable(BarricadeData bData, BarricadeDrop bDrop)
        {
            IsBarricade = true;
            barricadeData = bData;
            barricadeDrop = bDrop;
        }

        public Buildable(StructureData sData, StructureDrop sDrop)
        {
            IsBarricade = false;
            structureData = sData;
            structureDrop = sDrop;
        }
    }
}