using System.Collections.Generic;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Classes;
using Rocket.Core.Utils;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.API.Statics
{
    public static class Game
    {
        [NotNull]
        public static IEnumerable<Buildable> GetBuilds(CSteamID id)
        {
            var barricades = GetBarricades(id);
            var barricadeDrops = GetBarricadeDrops();
            var structures = GetStructures(id);
            var structureDrops = GetStructureDrops();

            var result = barricades.Select(k =>
            {
                var drop = barricadeDrops.FirstOrDefault(l => l.instanceID == k.instanceID);
                if (drop == null)
                    return new Buildable(k.angle_x, k.angle_y, k.angle_z, k.barricade.id, k.barricade.health,
                        k.instanceID, k.owner, k.group, k.point, k.barricade.asset, null, null, k.barricade.state);

                return new Buildable(k.angle_x, k.angle_y, k.angle_z, k.barricade.id, k.barricade.health, k.instanceID,
                    k.owner, k.group, k.point, k.barricade.asset, drop.model, drop.interactable, k.barricade.state);
            }).ToList();

            result.AddRange(structures.Select(k =>
            {
                var drop = structureDrops.FirstOrDefault(l => l.instanceID == k.instanceID);
                if (drop == null)
                    return new Buildable(k.angle_x, k.angle_y, k.angle_z, k.structure.id, k.structure.health,
                        k.instanceID, k.owner, k.group, k.point, k.structure.asset, null, null, null);

                return new Buildable(k.angle_x, k.angle_y, k.angle_z, k.structure.id, k.structure.health, k.instanceID,
                    k.owner, k.group, k.point, k.structure.asset, drop.model, null, null);
            }));

            return result;
        }

        [NotNull]
        public static IEnumerable<BarricadeDrop> GetBarricadeDrops()
        {
            var result = BarricadeManager.regions.Cast<BarricadeRegion>().SelectMany(brd => brd.drops).ToList();
            result.AddRange(BarricadeManager.plants.SelectMany(region => region.drops));

            return result;
        }

        [NotNull]
        public static IEnumerable<StructureDrop> GetStructureDrops()
        {
            var result = StructureManager.regions.Cast<StructureRegion>().SelectMany(brd => brd.drops).ToList();

            return result;
        }

        [NotNull]
        public static IEnumerable<BarricadeData> GetBarricades(CSteamID id)
        {
            var result = BarricadeManager.regions.Cast<BarricadeRegion>().SelectMany(brd => brd.barricades).ToList();
            result.AddRange(BarricadeManager.plants.SelectMany(region => region.barricades));

            return id == CSteamID.Nil ? result : result.Where(k => k.owner == (ulong) id);
        }

        [NotNull]
        public static IEnumerable<StructureData> GetStructures(CSteamID id)
        {
            var result = StructureManager.regions.Cast<StructureRegion>().SelectMany(brd => brd.structures).ToList();

            return id == CSteamID.Nil ? result : result.Where(k => k.owner == (ulong) id);
        }

        public static bool TryGetStructureRegion(Vector3 position, out byte x, out byte y, out ushort index,
            [CanBeNull] out StructureRegion region)
        {
            x = 0;
            y = 0;
            index = 0;
            region = null;

            if (!Regions.tryGetCoordinate(position, out x, out y)) return false;

            region = StructureManager.regions[x, y];
            var fIndex = region.structures.FindIndex(k => k.point == position);

            if (fIndex <= -1) return false;

            index = (ushort) fIndex;
            return true;
        }

        public static bool TryGetBarricadePlantAndRegion(Vector3 position, out byte x, out byte y, out ushort plant,
            out ushort index, [CanBeNull] out BarricadeRegion region)
        {
            x = 0;
            y = 0;
            plant = 0;
            index = 0;
            int fIndex;
            region = null;

            while (plant < BarricadeManager.plants.Count)
            {
                region = BarricadeManager.plants[plant];
                fIndex = region.barricades.FindIndex(k => k.point == position);

                if (fIndex > -1)
                {
                    index = (ushort) fIndex;
                    return true;
                }

                plant += 1;
            }

            plant = ushort.MaxValue;

            if (!Regions.tryGetCoordinate(position, out x, out y)) return false;

            region = BarricadeManager.regions[x, y];
            fIndex = region.barricades.FindIndex(k => k.point == position);

            if (fIndex <= -1) return false;

            index = (ushort) fIndex;
            return true;
        }

        public static void ChangeOwnerAndGroup(Vector3 position, ulong newOwner, ulong newGroup)
        {
            if (TryGetBarricadePlantAndRegion(position, out _, out _, out _, out var index,
                    out var bRegion))
                // ReSharper disable once PossibleNullReferenceException
                BarricadeManager.changeOwnerAndGroup(bRegion.drops[index].model, newOwner, newGroup);

            if (TryGetStructureRegion(position, out _, out _, out index, out var sRegion))
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
            if (TryGetBarricadePlantAndRegion(position, out var x, out var y, out var plant, out var index,
                    out var bRegion))
                // ReSharper disable once AssignNullToNotNullAttribute
                DamageBarricade(damage, x, y, plant, index, bRegion);

            if (!TryGetStructureRegion(position, out x, out y, out index, out var sRegion) ||
                // ReSharper disable once PossibleNullReferenceException
                !sRegion.structures.Exists(k => k.point == position)) return;

            DamageStructure(damage, x, y, index, sRegion, Vector3.zero);
        }

        public static void DamageStructure(ushort damage, byte x, byte y, ushort index,
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

        public static void DamageBarricade(ushort damage, byte x, byte y, ushort plant, ushort index,
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

        public static void SendBarricadeHealthChanged(byte x, byte y, ushort plant, ushort index,
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

        public static bool CheckToggle(ulong player, ulong group)
        {
            return !Dedicator.isDedicated && CheckToggle(Provider.client, player, Player.player.quests.groupID, group);
        }

        public static bool CheckToggle(CSteamID playerSource, ulong playerTarget, CSteamID groupSource,
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
            if (TryGetBarricadePlantAndRegion(position, out var x, out var y, out var plant, out var index,
                out var bRegion))
                BarricadeManager.destroyBarricade(bRegion, x, y, plant, index);

            if (!TryGetStructureRegion(position, out x, out y, out index, out var sRegion) ||
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

        public static void _repairBarricadeStructure(Vector3 position, float damage, float times)
        {
            if (TryGetBarricadePlantAndRegion(position, out _, out _, out _, out var index,
                    out var bRegion))
                // ReSharper disable once PossibleNullReferenceException
                BarricadeManager.repair(bRegion.drops[index].model, damage, times);

            if (!TryGetStructureRegion(position, out _, out _, out index, out var sRegion) ||
                // ReSharper disable once PossibleNullReferenceException
                !sRegion.structures.Exists(k => k.point == position)) return;

            StructureManager.repair(sRegion.drops[index].model, damage, times);
        }
    }
}