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

            var barricadeRegions = BarricadeManager.regions.Cast<BarricadeRegion>();

            if (includePlants)
                barricadeRegions = barricadeRegions.Concat(BarricadeManager.vehicleRegions);

            var structureRegions = StructureManager.regions.Cast<StructureRegion>();

            // ReSharper disable PossibleMultipleEnumeration
            var barricadeDatas = barricadeRegions.SelectMany(brd => brd.barricades);
            var barricadeDrops = barricadeRegions.SelectMany(brd => brd.drops);
            var structureDatas = structureRegions.SelectMany(str => str.structures);
            var structureDrops = structureRegions.SelectMany(str => str.drops);
            // ReSharper restore PossibleMultipleEnumeration

            return barricadeDatas
                .Select((k, i) =>
                {
                    var drop = barricadeDrops.ElementAt(i);
                    return drop == null ? null : new Buildable(k, drop);
                })
                .Concat(structureDatas.Select((k, i) =>
                {
                    var drop = structureDrops.ElementAt(i);
                    return drop == null ? null : new Buildable(k, drop);
                }))
                .Where(d => d != null)
                .ToList();
        }

        [NotNull]
        public static IEnumerable<BarricadeDrop> GetBarricadeDrops()
        {
            return BarricadeManager.regions.Cast<BarricadeRegion>().Concat(BarricadeManager.vehicleRegions)
                .SelectMany(k => k.drops);
        }

        [NotNull]
        public static IEnumerable<StructureDrop> GetStructureDrops()
        {
            return StructureManager.regions.Cast<StructureRegion>().SelectMany(brd => brd.drops);
        }

        [NotNull]
        public static IEnumerable<BarricadeData> GetBarricades(CSteamID id, bool includePlants)
        {
            var result = BarricadeManager.regions.Cast<BarricadeRegion>().SelectMany(k => k.barricades);

            if (includePlants)
                result = result.Concat(BarricadeManager.vehicleRegions.SelectMany(k => k.barricades));

            return id == CSteamID.Nil ? result : result.Where(k => k.owner == (ulong) id);
        }

        [NotNull]
        public static IEnumerable<StructureData> GetStructures(CSteamID id)
        {
            var result = StructureManager.regions.Cast<StructureRegion>().SelectMany(brd => brd.structures);

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

            while (plant < BarricadeManager.vehicleRegions.Count)
            {
                region = BarricadeManager.vehicleRegions[plant];
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