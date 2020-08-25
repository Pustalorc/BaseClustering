using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Classes;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.API.Statics
{
    public static class ReadOnlyGame
    {
        [NotNull]
        public static IEnumerable<Buildable> GetBuilds(CSteamID id, bool includePlants, bool usePreProcessedData)
        {
            if (usePreProcessedData && BaseClusteringPlugin.Instance != null)
                return BaseClusteringPlugin.Instance.Buildables.Where(
                    k => id == CSteamID.Nil || k.Owner == id.m_SteamID);

            var barricades = GetBarricades(id, includePlants);
            var barricadeDrops = GetBarricadeDrops();
            var structures = GetStructures(id);
            var structureDrops = GetStructureDrops();

            var result = barricades.Select(k =>
            {
                var drop = barricadeDrops.FirstOrDefault(l => l.instanceID == k.instanceID);
                return new Buildable(k, drop);
            }).ToList();

            result.AddRange(structures.Select(k =>
            {
                var drop = structureDrops.FirstOrDefault(l => l.instanceID == k.instanceID);
                return new Buildable(k, drop);
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
        public static IEnumerable<BarricadeData> GetBarricades(CSteamID id, bool includePlants)
        {
            var result = BarricadeManager.regions.Cast<BarricadeRegion>().SelectMany(brd => brd.barricades).ToList();

            if (includePlants)
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
    }
}