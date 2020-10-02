using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Classes;
using SDG.Unturned;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.API.Statics
{
    public static class ReadOnlyGame
    {
        [NotNull]
        public static IEnumerable<Buildable> GetBuilds(ulong owner = 0, ulong group = 0, bool includePlants = false,
            bool usePreProcessedData = true)
        {
            if (usePreProcessedData && BaseClusteringPlugin.Instance != null)
            {
                var processedBuildables = BaseClusteringPlugin.Instance.Buildables;

                if (owner == 0 && group == 0)
                    return processedBuildables;

                if (owner == 0)
                    return processedBuildables.Where(k => k.Group == group);

                return group == 0
                    ? processedBuildables.Where(k => k.Owner == owner)
                    : processedBuildables.Where(k => k.Owner == owner || k.Group == group);
            }

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

            var buildables = barricadeDatas
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
                .Where(d => d != null);

            if (owner == 0 && group == 0)
                return buildables;

            if (owner == 0)
                return buildables.Where(k => k.Group == group);

            return group == 0
                ? buildables.Where(k => k.Owner == owner)
                : buildables.Where(k => k.Owner == owner || k.Group == group);
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
        public static IEnumerable<BarricadeData> GetBarricades(bool includePlants)
        {
            var result = BarricadeManager.regions.Cast<BarricadeRegion>().SelectMany(k => k.barricades);

            if (includePlants)
                result = result.Concat(BarricadeManager.vehicleRegions.SelectMany(k => k.barricades));

            return result;
        }

        [NotNull]
        public static IEnumerable<StructureData> GetStructures()
        {
            return StructureManager.regions.Cast<StructureRegion>().SelectMany(brd => brd.structures);
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