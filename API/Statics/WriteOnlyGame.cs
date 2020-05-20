using System.Linq;
using System.Threading;
using Rocket.Core.Utils;
using SDG.Unturned;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.API.Statics
{
    public static class WriteOnlyGame
    {
        public static void ChangeOwnerAndGroup(Vector3 position, ulong newOwner, ulong newGroup)
        {
            if (Thread.CurrentThread.IsGameThread())
                _changeOwnerAndGroup(position, newOwner, newGroup);
            else
                TaskDispatcher.QueueOnMainThread(() => _changeOwnerAndGroup(position, newOwner, newGroup));
        }

        private static void _changeOwnerAndGroup(Vector3 position, ulong newOwner, ulong newGroup)
        {
            if (ReadOnlyGame.TryGetBarricadePlantAndRegion(position, out _, out _, out _, out var index,
                    out var bRegion))
                // ReSharper disable once PossibleNullReferenceException
                BarricadeManager.changeOwnerAndGroup(bRegion.drops[index].model, newOwner, newGroup);

            if (ReadOnlyGame.TryGetStructureRegion(position, out _, out _, out index, out var sRegion))
                // ReSharper disable once PossibleNullReferenceException
                StructureManager.changeOwnerAndGroup(sRegion.drops[index].model, newOwner, newGroup);
        }

        public static void DamageBarricadeStructure(Vector3 position, ushort damage)
        {
            if (Thread.CurrentThread.IsGameThread())
                _damageBarricadeStructure(position, damage);
            else
                TaskDispatcher.QueueOnMainThread(() => _damageBarricadeStructure(position, damage));
        }

        private static void _damageBarricadeStructure(Vector3 position, ushort damage)
        {
            if (ReadOnlyGame.TryGetBarricadePlantAndRegion(position, out _, out _, out _, out var index,
                out var bRegion) && bRegion != null)
                BarricadeManager.damage(
                    bRegion.drops.FirstOrDefault(k => k.instanceID == bRegion.barricades[index].instanceID)?.model,
                    damage, 1, false);

            if (!ReadOnlyGame.TryGetStructureRegion(position, out _, out _, out index, out var sRegion) ||
                sRegion?.structures.Exists(k => k.point == position) == false) return;

            StructureManager.damage(
                sRegion?.drops.FirstOrDefault(k => k.instanceID == sRegion.structures[index].instanceID)?.model,
                Vector3.zero, damage, 1, false);
        }

        public static void RemoveBarricadeStructure(Vector3 position)
        {
            if (Thread.CurrentThread.IsGameThread())
                _removeBarricadeStructure(position);
            else
                TaskDispatcher.QueueOnMainThread(() => _removeBarricadeStructure(position));
        }

        public static void RemoveBarricadeStructure(Transform model)
        {
            if (Thread.CurrentThread.IsGameThread())
                _removeBarricadeStructure(model);
            else
                TaskDispatcher.QueueOnMainThread(() => _removeBarricadeStructure(model));
        }

        private static void _removeBarricadeStructure(Vector3 position)
        {
            if (ReadOnlyGame.TryGetBarricadePlantAndRegion(position, out var x, out var y, out var plant, out var index,
                out var bRegion))
            {
                BarricadeManager.destroyBarricade(bRegion, x, y, plant, index);
                return;
            }

            if (ReadOnlyGame.TryGetStructureRegion(position, out x, out y, out index, out var sRegion))
                StructureManager.destroyStructure(sRegion, x, y, index, Vector3.zero);
        }

        private static void _removeBarricadeStructure(Transform model)
        {
            if (BarricadeManager.tryGetInfo(model, out var x, out var y, out var plant, out var index, out var bRegion))
            {
                BarricadeManager.destroyBarricade(bRegion, x, y, plant, index);
                return;
            }

            if (StructureManager.tryGetInfo(model, out x, out y, out index, out var sRegion))
                StructureManager.destroyStructure(sRegion, x, y, index, Vector3.zero);
        }

        public static void RepairBarricadeStructure(Vector3 position, float damage, float times)
        {
            if (Thread.CurrentThread.IsGameThread())
                _repairBarricadeStructure(position, damage, times);
            else
                TaskDispatcher.QueueOnMainThread(() => _repairBarricadeStructure(position, damage, times));
        }

        private static void _repairBarricadeStructure(Vector3 position, float damage, float times)
        {
            if (ReadOnlyGame.TryGetBarricadePlantAndRegion(position, out _, out _, out _, out var index,
                    out var bRegion))
                // ReSharper disable once PossibleNullReferenceException
                BarricadeManager.repair(bRegion.drops[index].model, damage, times);

            if (!ReadOnlyGame.TryGetStructureRegion(position, out _, out _, out index, out var sRegion) ||
                // ReSharper disable once PossibleNullReferenceException
                !sRegion.structures.Exists(k => k.point == position)) return;

            StructureManager.repair(sRegion.drops[index].model, damage, times);
        }
    }
}