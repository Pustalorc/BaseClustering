using JetBrains.Annotations;
using SDG.Unturned;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.API.Classes
{
    // This references the original objects. Changes done to them should be reflected in the property accessors here.
    public class Buildable
    {
        private BarricadeData m_BarricadeData;
        private BarricadeDrop m_BarricadeDrop;
        private StructureData m_StructureData;
        private StructureDrop m_StructureDrop;
        private bool m_IsBarricade;

        // Barricade.cs / Structure.cs
        public ushort AssetId => Asset.id;
        public ushort Health => m_IsBarricade ? m_BarricadeData.barricade.health : m_StructureData.structure.health;
        [CanBeNull] public byte[] State => m_IsBarricade ? m_BarricadeData.barricade.state : null;

        // BarricadeData.cs / StructureData.cs
        public ulong Owner => m_IsBarricade ? m_BarricadeData.owner : m_StructureData.owner;
        public ulong Group => m_IsBarricade ? m_BarricadeData.group : m_StructureData.group;
        public byte AngleX => m_IsBarricade ? m_BarricadeData.angle_x : m_StructureData.angle_x;
        public byte AngleY => m_IsBarricade ? m_BarricadeData.angle_y : m_StructureData.angle_y;
        public byte AngleZ => m_IsBarricade ? m_BarricadeData.angle_z : m_StructureData.angle_z;
        public Vector3 Position => m_IsBarricade ? m_BarricadeData.point : m_StructureData.point;

        // BarricadeDrop.cs / StructureDrop.cs
        public Transform Model => m_IsBarricade ? m_BarricadeDrop.model : m_StructureDrop.model;
        [CanBeNull] public Interactable Interactable => m_IsBarricade ? m_BarricadeDrop.interactable : null;

        // Multiple Files
        public uint InstanceId => m_IsBarricade ? m_BarricadeData.instanceID : m_StructureData.instanceID;
        public Asset Asset => m_IsBarricade ? (Asset) m_BarricadeData.barricade.asset : m_StructureData.structure.asset;

        public Buildable(BarricadeData bData, BarricadeDrop bDrop)
        {
            m_IsBarricade = true;
            m_BarricadeData = bData;
            m_BarricadeDrop = bDrop;
        }

        public Buildable(StructureData sData, StructureDrop sDrop)
        {
            m_IsBarricade = false;
            m_StructureData = sData;
            m_StructureDrop = sDrop;
        }
    }
}