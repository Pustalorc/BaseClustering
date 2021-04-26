using System;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Utilities;
using SDG.Unturned;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.API.Buildables
{
    public sealed class StructureBuildable : Buildable
    {
        private readonly StructureData m_StructureData;
        private readonly StructureDrop m_StructureDrop;

        public StructureBuildable(StructureData data, StructureDrop drop)
        {
            m_StructureData = data;
            m_StructureDrop = drop;
        }

        public override ushort AssetId => Asset.id;

        public override ushort Health => m_StructureData.structure.health;

        [CanBeNull] public override byte[] State => null;

        public override ulong Owner => m_StructureData.owner;

        public override ulong Group => m_StructureData.group;

        public override byte AngleX => m_StructureData.angle_x;

        public override byte AngleY => m_StructureData.angle_y;

        public override byte AngleZ => m_StructureData.angle_z;

        public override Vector3 Position => m_StructureData.point;

        public override Transform Model => m_StructureDrop.model;

        [CanBeNull] public override Interactable Interactable => null;

        public override uint InstanceId => m_StructureData.instanceID;

        public override Asset Asset => m_StructureData.structure.asset;

        public override bool IsPlanted => false;

        public override void UnsafeDestroy()
        {
            ThreadUtil.assertIsGameThread();
            if (!StructureManager.tryGetInfo(Model, out var x, out var y, out var index, out var sRegion))
                return;

            StructureManager.destroyStructure(sRegion, x, y, index, Vector3.zero);
        }

        public override void UnsafeDamage(ushort damage)
        {
            ThreadUtil.assertIsGameThread();
            StructureManager.damage(Model, Vector3.zero, damage, 1, false, damageOrigin: EDamageOrigin.Unknown);
        }

        public override void UnsafeHeal(ushort amount)
        {
            ThreadUtil.assertIsGameThread();
            StructureManager.repair(Model, amount, 1);
        }
    }
}