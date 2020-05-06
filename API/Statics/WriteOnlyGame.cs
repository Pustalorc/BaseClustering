using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using Rocket.Core.Utils;
using SDG.Unturned;
using Steamworks;
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
            if (ReadOnlyGame.TryGetBarricadePlantAndRegion(position, out var x, out var y, out var plant, out var index,
                    out var bRegion))
                // ReSharper disable once AssignNullToNotNullAttribute
                DamageBarricade(damage, x, y, plant, index, bRegion);

            if (!ReadOnlyGame.TryGetStructureRegion(position, out x, out y, out index, out var sRegion) ||
                // ReSharper disable once PossibleNullReferenceException
                !sRegion.structures.Exists(k => k.point == position)) return;

            DamageStructure(damage, x, y, index, sRegion, Vector3.zero);
        }

        private static void DamageStructure(ushort damage, byte x, byte y, ushort index,
            [NotNull] StructureRegion region,
            Vector3 direction)
        {
            using (new StructureRegionSyncTest(region, "damage"))
            {
                var structure = region.structures[index];

                if (structure.structure.isDead) return;

                var itemStructureAsset = (ItemStructureAsset) Assets.find(EAssetType.ITEM, structure.structure.id);
                if (itemStructureAsset == null) return;

                structure.structure.askDamage(damage);
                if (structure.structure.isDead)
                {
                    if (itemStructureAsset.explosion != 0)
                        EffectManager.sendEffect(itemStructureAsset.explosion, EffectManager.SMALL,
                            structure.point + Vector3.down * StructureManager.HEIGHT);
                    StructureManager.destroyStructure(region, x, y, index, direction * damage);
                }
                else
                {
                    SendStructureHealthChanged(x, y, index, region);
                }
            }
        }

        private static void DamageBarricade(ushort damage, byte x, byte y, ushort plant, ushort index,
            [NotNull] BarricadeRegion bRegion)
        {
            using (new BarricadeRegionSyncTest(bRegion, "damage"))
            {
                var barricade = bRegion.barricades[index];

                if (barricade.barricade.isDead) return;

                var itemBarricadeAsset = (ItemBarricadeAsset) Assets.find(EAssetType.ITEM, barricade.barricade.id);
                if (itemBarricadeAsset == null) return;

                bRegion.barricades[index].barricade.askDamage(damage);
                if (bRegion.barricades[index].barricade.isDead)
                {
                    if (itemBarricadeAsset.explosion != 0)
                    {
                        if (plant == 65535)
                            EffectManager.sendEffect(itemBarricadeAsset.explosion, x, y,
                                BarricadeManager.BARRICADE_REGIONS,
                                barricade.point + Vector3.down * itemBarricadeAsset.offset);
                        else
                            EffectManager.sendEffect(itemBarricadeAsset.explosion, EffectManager.MEDIUM,
                                barricade.point + Vector3.down * itemBarricadeAsset.offset);
                    }

                    BarricadeManager.destroyBarricade(bRegion, x, y, plant, index);
                }
                else
                {
                    SendBarricadeHealthChanged(x, y, plant, index, bRegion);
                }
            }
        }

        private static void SendStructureHealthChanged(byte x, byte y, ushort index, StructureRegion region)
        {
            foreach (var client in Provider.clients.Where(client => client.player != null &&
                                                                    CheckToggle(client.playerID.steamID,
                                                                        region.structures[index].owner,
                                                                        client.player.quests.groupID,
                                                                        region.structures[index].group) &&
                                                                    Regions.checkArea(x, y,
                                                                        client.player.movement.region_x,
                                                                        client.player.movement.region_y,
                                                                        StructureManager.STRUCTURE_REGIONS)))
                StructureManager.instance.channel.send("tellStructureHealth", client.playerID.steamID,
                    ESteamPacket.UPDATE_UNRELIABLE_BUFFER, x, y, index, (byte) Mathf.RoundToInt(
                        region.structures[index].structure.health /
                        (float) region.structures[index].structure.asset.health * 100f));
        }

        private static void SendBarricadeHealthChanged(byte x, byte y, ushort plant, ushort index,
            [NotNull] BarricadeRegion region)
        {
            var barricade = region.barricades[index];

            foreach (var client in Provider.clients.Where(client => CheckToggle(client.playerID.steamID,
                barricade.owner, client.player.quests.groupID,
                barricade.group)))
                if (plant == 65535)
                {
                    if (client.player != null && Regions.checkArea(x, y, client.player.movement.region_x,
                        client.player.movement.region_y, BarricadeManager.BARRICADE_REGIONS))
                        BarricadeManager.instance.channel.send("tellBarricadeHealth", client.playerID.steamID,
                            ESteamPacket.UPDATE_UNRELIABLE_BUFFER, x, y, plant, index,
                            (byte) (barricade.barricade.health / (float) barricade.barricade.asset.health *
                                    100f));
                }
                else
                {
                    BarricadeManager.instance.channel.send("tellBarricadeHealth", client.playerID.steamID,
                        ESteamPacket.UPDATE_UNRELIABLE_BUFFER, x, y, plant, index,
                        (byte) (barricade.barricade.health / (float) barricade.barricade.asset.health * 100f));
                }
        }

        private static bool CheckToggle(CSteamID playerSource, ulong playerTarget, CSteamID groupSource,
            ulong groupTarget)
        {
            return Provider.isServer && !Dedicator.isDedicated || playerSource.m_SteamID == playerTarget ||
                   groupSource != CSteamID.Nil && groupSource.m_SteamID == groupTarget;
        }

        public static void RemoveBarricadeStructure(Vector3 position)
        {
            if (Thread.CurrentThread.IsGameThread())
                _removeBarricadeStructure(position);
            else
                TaskDispatcher.QueueOnMainThread(() => _removeBarricadeStructure(position));
        }

        private static void _removeBarricadeStructure(Vector3 position)
        {
            if (ReadOnlyGame.TryGetBarricadePlantAndRegion(position, out var x, out var y, out var plant, out var index,
                out var bRegion))
                BarricadeManager.destroyBarricade(bRegion, x, y, plant, index);

            if (!ReadOnlyGame.TryGetStructureRegion(position, out x, out y, out index, out var sRegion) ||
                // ReSharper disable once PossibleNullReferenceException
                !sRegion.structures.Exists(k => k.point == position)) return;

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