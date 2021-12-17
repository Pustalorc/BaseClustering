using SDG.Unturned;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.API.Buildables;

/// <inheritdoc />
public sealed class BarricadeBuildable : Buildable
{
    /// <summary>
    /// Server-Side barricade data.
    /// </summary>
    public BarricadeData BarricadeData { get; }

    /// <summary>
    /// The drop/model of the barricade.
    /// </summary>
    public BarricadeDrop BarricadeDrop { get; }

    /// <summary>
    /// Creates a new instance of <see cref="BarricadeBuildable"/> with the specified drop.
    /// </summary>
    /// <param name="drop">The drop to add.</param>
    public BarricadeBuildable(BarricadeDrop drop)
    {
        BarricadeDrop = drop;
        BarricadeData = drop.GetServersideData();
    }

    /// <inheritdoc />
    public override ushort AssetId => Asset.id;

    /// <inheritdoc />
    public override ushort Health => BarricadeData.barricade.health;

    /// <inheritdoc />
    public override byte[]? State => BarricadeData.barricade.state;

    /// <inheritdoc />
    public override ulong Owner => BarricadeData.owner;

    /// <inheritdoc />
    public override ulong Group => BarricadeData.group;

    /// <inheritdoc />
    public override byte AngleX => BarricadeData.angle_x;

    /// <inheritdoc />
    public override byte AngleY => BarricadeData.angle_y;

    /// <inheritdoc />
    public override byte AngleZ => BarricadeData.angle_z;

    /// <inheritdoc />
    public override Vector3 Position => BarricadeData.point;

    /// <inheritdoc />
    public override Transform Model => BarricadeDrop.model;

    /// <inheritdoc />
    public override Interactable? Interactable => BarricadeDrop.interactable;

    /// <inheritdoc />
    public override uint InstanceId => BarricadeData.instanceID;

    /// <inheritdoc />
    public override Asset Asset => BarricadeDrop.asset;

    /// <inheritdoc />
    public override bool IsPlanted => BarricadeDrop.model != null &&
                                      BarricadeDrop.model.parent != null &&
                                      BarricadeDrop.model.parent.CompareTag("Vehicle");

    /// <inheritdoc />
    public override void UnsafeDestroy()
    {
        ThreadUtil.assertIsGameThread();
        if (!BarricadeManager.tryGetRegion(Model, out var x, out var y, out var plant, out _))
            return;

        BarricadeManager.destroyBarricade(BarricadeDrop, x, y, plant);
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