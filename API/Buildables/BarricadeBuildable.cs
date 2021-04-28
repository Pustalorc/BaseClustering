using SDG.Unturned;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.API.Buildables
{
    public sealed class BarricadeBuildable : Buildable
    {
        private readonly BarricadeData m_BarricadeData;
        private readonly BarricadeDrop m_BarricadeDrop;

        public BarricadeBuildable(BarricadeData data, BarricadeDrop drop)
        {
            m_BarricadeData = data;
            m_BarricadeDrop = drop;
        }

        public override ushort AssetId => Asset.id;

        public override ushort Health => m_BarricadeData.barricade.health;

        public override byte[] State => m_BarricadeData.barricade.state;

        public override ulong Owner => m_BarricadeData.owner;
        public override ulong Group => m_BarricadeData.group;

        public override byte AngleX => m_BarricadeData.angle_x;

        public override byte AngleY => m_BarricadeData.angle_y;

        public override byte AngleZ => m_BarricadeData.angle_z;

        public override Vector3 Position => m_BarricadeData.point;

        public override Transform Model => m_BarricadeDrop.model;

        public override Interactable Interactable => m_BarricadeDrop.interactable;

        public override uint InstanceId => m_BarricadeData.instanceID;

        public override Asset Asset => m_BarricadeDrop.asset;

        public override bool IsPlanted => m_BarricadeDrop != null && m_BarricadeDrop.model != null &&
                                          m_BarricadeDrop.model.parent != null &&
                                          m_BarricadeDrop.model.parent.CompareTag("Vehicle");

        public override void UnsafeDestroy()
        {
            ThreadUtil.assertIsGameThread();
            if (!BarricadeManager.tryGetInfo(Model, out var x, out var y, out var plant, out var index,
                out var bRegion))
                return;

            BarricadeManager.destroyBarricade(bRegion, x, y, plant, index);
        }

        public override void UnsafeDamage(ushort damage)
        {
            ThreadUtil.assertIsGameThread();
            BarricadeManager.damage(Model, damage, 1, false, damageOrigin: EDamageOrigin.Unknown);
        }

        public override void UnsafeHeal(ushort amount)
        {
            ThreadUtil.assertIsGameThread();
            BarricadeManager.repair(Model, amount, 1);
        }
    }
}