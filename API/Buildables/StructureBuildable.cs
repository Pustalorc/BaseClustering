using SDG.Unturned;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.API.Buildables
{
    /// <inheritdoc />
    public sealed class StructureBuildable : Buildable
    {
        /// <summary>
        /// Server-Side structure data.
        /// </summary>
        public StructureData StructureData { get; }

        /// <summary>
        /// The drop/model of the structure.
        /// </summary>
        public StructureDrop StructureDrop { get; }

        /// <summary>
        /// Creates a new instance of <see cref="StructureBuildable"/> with the specified data and drop.
        /// </summary>
        /// <param name="drop">The drop to add.</param>
        public StructureBuildable(StructureDrop drop)
        {
            StructureDrop = drop;
            StructureData = drop.GetServersideData();
        }

        /// <inheritdoc />
        public override ushort AssetId => Asset.id;

        /// <inheritdoc />
        public override ushort Health => StructureData.structure.health;

        /// <inheritdoc />
        public override byte[]? State => null;

        /// <inheritdoc />
        public override ulong Owner => StructureData.owner;

        /// <inheritdoc />
        public override ulong Group => StructureData.group;

        /// <inheritdoc />
        public override byte AngleX => StructureData.angle_x;

        /// <inheritdoc />
        public override byte AngleY => StructureData.angle_y;

        /// <inheritdoc />
        public override byte AngleZ => StructureData.angle_z;

        /// <inheritdoc />
        public override Vector3 Position => StructureData.point;

        /// <inheritdoc />
        public override Transform Model => StructureDrop.model;

        /// <inheritdoc />
        public override Interactable? Interactable => null;

        /// <inheritdoc />
        public override uint InstanceId => StructureData.instanceID;

        /// <inheritdoc />
        public override Asset Asset => StructureData.structure.asset;

        /// <inheritdoc />
        public override bool IsPlanted => false;

        /// <inheritdoc />
        public override void UnsafeDestroy()
        {
            ThreadUtil.assertIsGameThread();
            if (!StructureManager.tryGetRegion(Model, out var x, out var y, out _))
                return;

            StructureManager.destroyStructure(StructureDrop, x, y, Vector3.zero);
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