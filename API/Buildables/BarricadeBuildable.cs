using SDG.Unturned;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.API.Buildables
{
    /// <inheritdoc />
    public sealed class BarricadeBuildable : Buildable
    {
        private readonly BarricadeData m_BarricadeData;
        private readonly BarricadeDrop m_BarricadeDrop;

        /// <summary>
        /// Creates a new instance of <see cref="BarricadeBuildable"/> with the specified data and drop.
        /// </summary>
        /// <param name="data">The data to add.</param>
        /// <param name="drop">The drop to add.</param>
        public BarricadeBuildable(BarricadeData data, BarricadeDrop drop)
        {
            m_BarricadeData = data;
            m_BarricadeDrop = drop;
        }

        /// <inheritdoc />
        public override ushort AssetId => Asset.id;

        /// <inheritdoc />
        public override ushort Health => m_BarricadeData.barricade.health;

        /// <inheritdoc />
        public override byte[]? State => m_BarricadeData.barricade.state;

        /// <inheritdoc />
        public override ulong Owner => m_BarricadeData.owner;

        /// <inheritdoc />
        public override ulong Group => m_BarricadeData.group;

        /// <inheritdoc />
        public override byte AngleX => m_BarricadeData.angle_x;

        /// <inheritdoc />
        public override byte AngleY => m_BarricadeData.angle_y;

        /// <inheritdoc />
        public override byte AngleZ => m_BarricadeData.angle_z;

        /// <inheritdoc />
        public override Vector3 Position => m_BarricadeData.point;

        /// <inheritdoc />
        public override Transform Model => m_BarricadeDrop.model;

        /// <inheritdoc />
        public override Interactable? Interactable => m_BarricadeDrop.interactable;

        /// <inheritdoc />
        public override uint InstanceId => m_BarricadeData.instanceID;

        /// <inheritdoc />
        public override Asset Asset => m_BarricadeDrop.asset;

        /// <inheritdoc />
        public override bool IsPlanted => m_BarricadeDrop.model != null &&
                                          m_BarricadeDrop.model.parent != null &&
                                          m_BarricadeDrop.model.parent.CompareTag("Vehicle");

        /// <inheritdoc />
        public override void UnsafeDestroy()
        {
            ThreadUtil.assertIsGameThread();
            if (!BarricadeManager.tryGetInfo(Model, out var x, out var y, out var plant, out var index,
                out var bRegion))
                return;

            BarricadeManager.destroyBarricade(bRegion, x, y, plant, index);
        }

        /// <inheritdoc />
        public override void UnsafeDamage(ushort damage)
        {
            ThreadUtil.assertIsGameThread();
            BarricadeManager.damage(Model, damage, 1, false, damageOrigin: EDamageOrigin.Unknown);
        }

        /// <inheritdoc />
        public override void UnsafeHeal(ushort amount)
        {
            ThreadUtil.assertIsGameThread();
            BarricadeManager.repair(Model, amount, 1);
        }
    }
}