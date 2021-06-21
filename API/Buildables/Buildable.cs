using System;
using System.Threading;
using JetBrains.Annotations;
using Rocket.Core.Utils;
using SDG.Unturned;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.API.Buildables
{
    /// <summary>
    /// An abstraction of both barricades and structures from unturned.
    /// </summary>
    /// <remarks>
    /// It specifically abstracts the following classes into one:
    /// <br/>
    /// <see cref="Barricade"/> <see cref="Structure"/> <see cref="BarricadeData"/> <see cref="StructureData"/>
    /// <see cref="BarricadeDrop"/> <see cref="StructureDrop"/>
    /// <br/>
    /// It also provides extra methods to interact with a buildable.
    /// </remarks>
    public abstract class Buildable
    {
        // Barricade.cs / Structure.cs

        /// <summary>
        /// The Id of the Asset of this buildable.
        /// </summary>
        public abstract ushort AssetId { get; }

        /// <summary>
        /// The amount of health this buildable has.
        /// </summary>
        [UsedImplicitly]
        public abstract ushort Health { get; }

        /// <summary>
        /// The state of the buildable.
        /// </summary>
        /// <remarks>
        /// This is <see cref="Nullable"/> because <see cref="Structure"/> does not have any State information.
        /// </remarks>
        [UsedImplicitly]
        public abstract byte[]? State { get; }


        // BarricadeData.cs / StructureData.cs

        /// <summary>
        /// The owner of this buildable.
        /// </summary>
        public abstract ulong Owner { get; }

        /// <summary>
        /// The group set to this buildable.
        /// </summary>
        public abstract ulong Group { get; }

        /// <summary>
        /// The angle of rotation on the X axis of this buildable.
        /// </summary>
        [UsedImplicitly]
        public abstract byte AngleX { get; }

        /// <summary>
        /// The angle of rotation on the Y axis of this buildable.
        /// </summary>
        [UsedImplicitly]
        public abstract byte AngleY { get; }

        /// <summary>
        /// The angle of rotation on the Z axis of this buildable.
        /// </summary>
        [UsedImplicitly]
        public abstract byte AngleZ { get; }

        /// <summary>
        /// The position as a <see cref="Vector3"/> of this buildable.
        /// </summary>
        public abstract Vector3 Position { get; }


        // BarricadeDrop.cs / StructureDrop.cs

        /// <summary>
        /// The model (<see cref="Transform"/>) of this buildable.
        /// </summary>
        public abstract Transform Model { get; }

        /// <summary>
        /// The <see cref="Interactable"/> instance of this buildable.
        /// </summary>
        /// <remarks>
        /// This is <see cref="Nullable"/> because <see cref="StructureDrop"/> does not have any Interactable information.
        /// </remarks>
        public abstract Interactable? Interactable { get; }


        // Multiple Files

        /// <summary>
        /// The unique instance Id of this buildable, set by unturned.
        /// </summary>
        public abstract uint InstanceId { get; }

        /// <summary>
        /// The <see cref="Asset"/> of this buildable.
        /// </summary>
        public abstract Asset Asset { get; }


        // Custom

        /// <summary>
        /// This determines if the buildable is planted (on a vehicle).
        /// </summary>
        /// <remarks>
        /// This will always return <see langword="false"/> if its a <see cref="StructureBuildable"/>.
        /// </remarks>
        public abstract bool IsPlanted { get; }

        /// <summary>
        /// Destroys this buildable without checking if we are on the main thread.
        /// </summary>
        /// <remarks>
        /// If this method is ran on a separate thread and there's another read/write operation happening at the same time that we are destroying, the game will crash.
        /// </remarks>
        [UsedImplicitly]
        public abstract void UnsafeDestroy();

        /// <summary>
        /// Safely destroys this buildable no matter which thread it is called from.
        /// </summary>
        public void SafeDestroy()
        {
            if (!Thread.CurrentThread.IsGameThread())
            {
                TaskDispatcher.QueueOnMainThread(UnsafeDestroy);
                return;
            }

            UnsafeDestroy();
        }

        /// <summary>
        /// Damages this buildable without checking if we are on the main thread.
        /// </summary>
        /// <param name="damage">The amount of damage to apply to the buildable.</param>
        /// <remarks>
        /// If this method is ran on a separate thread and there's another read/write operation happening at the same time that we are damaging (and possibly destroying), the game will crash.
        /// <br/>
        /// <paramref name="damage"/> is in raw damage, not a percentage of max health.
        /// </remarks>
        [UsedImplicitly]
        public abstract void UnsafeDamage(ushort damage);

        /// <summary>
        /// Safely damages this buildable no matter which thread it is called from.
        /// </summary>
        /// <param name="damage">The amount of damage to apply to the buildable.</param>
        /// <remarks>
        /// <paramref name="damage"/> is in raw damage, not a percentage of max health.
        /// </remarks>
        [UsedImplicitly]
        public void SafeDamage(ushort damage)
        {
            if (!Thread.CurrentThread.IsGameThread())
            {
                TaskDispatcher.QueueOnMainThread(() => UnsafeDamage(damage));
                return;
            }

            UnsafeDamage(damage);
        }

        /// <summary>
        /// Heals this buildable without checking if we are on the main thread.
        /// </summary>
        /// <param name="amount">The amount of healing to apply to the buildable.</param>
        /// <remarks>
        /// If this method is ran on a separate thread and there's another read/write operation happening at the same time that we are healing, there's a high chance that the game will crash.
        /// <br/>
        /// <paramref name="amount"/> is in raw healing, not a percentage of max health.
        /// </remarks>
        [UsedImplicitly]
        public abstract void UnsafeHeal(ushort amount);

        /// <summary>
        /// Safely heals this buildable no matter which thread it is called from.
        /// </summary>
        /// <param name="amount">The amount of healing to apply to the buildable.</param>
        /// <remarks>
        /// <paramref name="amount"/> is in raw healing, not a percentage of max health.
        /// </remarks>
        [UsedImplicitly]
        public void SafeHeal(ushort amount)
        {
            if (!Thread.CurrentThread.IsGameThread())
            {
                TaskDispatcher.QueueOnMainThread(() => UnsafeHeal(amount));
                return;
            }

            UnsafeHeal(amount);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Buildable [{AssetId}:{InstanceId}] located at {Position}";
        }
    }
}