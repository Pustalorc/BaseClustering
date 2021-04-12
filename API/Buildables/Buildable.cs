using System.Threading;
using Rocket.Core.Utils;
using SDG.Unturned;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.API.Buildables
{
    // This references the original objects. Changes done to them should be reflected in the property accessors here.
    public abstract class Buildable
    {
        // Barricade.cs / Structure.cs
        public abstract ushort AssetId { get; }
        public abstract ushort Health { get; }
        public abstract byte[] State { get; }

        // BarricadeData.cs / StructureData.cs
        public abstract ulong Owner { get; }
        public abstract ulong Group { get; }
        public abstract byte AngleX { get; }
        public abstract byte AngleY { get; }
        public abstract byte AngleZ { get; }
        public abstract Vector3 Position { get; }

        // BarricadeDrop.cs / StructureDrop.cs
        public abstract Transform Model { get; }
        public abstract Interactable Interactable { get; }

        // Multiple Files
        public abstract uint InstanceId { get; }
        public abstract Asset Asset { get; }

        // Custom
        public abstract bool IsPlanted { get; }

        // ReSharper disable once MemberCanBeProtected.Global
        // Public availability of this method through Buildable type is wanted and expected.
        public abstract void UnsafeDestroy();

        public void SafeDestroy()
        {
            if (!Thread.CurrentThread.IsGameThread())
            {
                TaskDispatcher.QueueOnMainThread(UnsafeDestroy);
                return;
            }

            UnsafeDestroy();
        }

        // ReSharper disable once MemberCanBeProtected.Global
        // Public availability of this method through Buildable type is wanted and expected.
        public abstract void UnsafeDamage(ushort damage);

        public void SafeDamage(ushort damage)
        {
            if (!Thread.CurrentThread.IsGameThread())
            {
                TaskDispatcher.QueueOnMainThread(() => UnsafeDamage(damage));
                return;
            }

            UnsafeDamage(damage);
        }

        // ReSharper disable once MemberCanBeProtected.Global
        // Public availability of this method through Buildable type is wanted and expected.
        public abstract void UnsafeHeal(ushort amount);

        public void SafeHeal(ushort amount)
        {
            if (!Thread.CurrentThread.IsGameThread())
            {
                TaskDispatcher.QueueOnMainThread(() => UnsafeHeal(amount));
                return;
            }

            UnsafeHeal(amount);
        }

        public override string ToString()
        {
            return $"Buildable [{AssetId}:{InstanceId}] located at {Position}";
        }
    }
}