using SDG.Unturned;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.API.Buildables
{
    /// <inheritdoc />
    public sealed class StructureBuildable : Buildable
    {
        private readonly StructureData m_StructureData;
        private readonly StructureDrop m_StructureDrop;

        /// <summary>
        /// Creates a new instance of <see cref="StructureBuildable"/> with the specified data and drop.
        /// </summary>
        /// <param name="data">The data to add.</param>
        /// <param name="drop">The drop to add.</param>
        public StructureBuildable(StructureData data, StructureDrop drop)
        {
            m_StructureData = data;
            m_StructureDrop = drop;
        }

        /// <inheritdoc />
        public override ushort AssetId => Asset.id;

        /// <inheritdoc />
        public override ushort Health => m_StructureData.structure.health;

        /// <inheritdoc />
        public override byte[]? State => null;

        /// <inheritdoc />
        public override ulong Owner => m_StructureData.owner;

        /// <inheritdoc />
        public override ulong Group => m_StructureData.group;

        /// <inheritdoc />
        public override byte AngleX => m_StructureData.angle_x;

        /// <inheritdoc />
        public override byte AngleY => m_StructureData.angle_y;

        /// <inheritdoc />
        public override byte AngleZ => m_StructureData.angle_z;

        /// <inheritdoc />
        public override Vector3 Position => m_StructureData.point;

        /// <inheritdoc />
        public override Transform Model => m_StructureDrop.model;

        /// <inheritdoc />
        public override Interactable? Interactable => null;

        /// <inheritdoc />
        public override uint InstanceId => m_StructureData.instanceID;

        /// <inheritdoc />
        public override Asset Asset => m_StructureData.structure.asset;

        /// <inheritdoc />
        public override bool IsPlanted => false;

        /// <inheritdoc />
        public override void UnsafeDestroy()
        {
            ThreadUtil.assertIsGameThread();
            if (!StructureManager.tryGetInfo(Model, out var x, out var y, out var index, out var sRegion))
                return;

            StructureManager.destroyStructure(sRegion, x, y, index, Vector3.zero);
        }

        /// <inheritdoc />
        public override void UnsafeDamage(ushort damage)
        {
            ThreadUtil.assertIsGameThread();
            StructureManager.damage(Model, Vector3.zero, damage, 1, false, damageOrigin: EDamageOrigin.Unknown);
        }

        /// <inheritdoc />
        public override void UnsafeHeal(ushort amount)
        {
            ThreadUtil.assertIsGameThread();
            StructureManager.repair(Model, amount, 1);
        }
    }
}