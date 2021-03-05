using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SDG.Unturned;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.API.Buildables
{
    public static class BuildableCollection
    {
        [NotNull]
        public static IEnumerable<Buildable> GetBuildables(ulong owner = 0, ulong group = 0,
            bool includePlants = false, bool useProcessedData = true)
        {
            if (useProcessedData && BaseClusteringPlugin.Instance != null)
            {
                var processedBuildables = BaseClusteringPlugin.Instance.PostProcessedBuildables.AsEnumerable();

                // ReSharper disable once InvertIf
                // This is a stupid invert if.
                if (includePlants)
                {
                    var regions = BarricadeManager.vehicleRegions;
                    var bDatas = regions.SelectMany(brd => brd.barricades).ToList();
                    var bDrops = regions.SelectMany(brd => brd.drops).ToList();
                    processedBuildables = processedBuildables.Concat(bDatas.Select((k, i) =>
                    {
                        var drop = bDrops.ElementAt(i);
                        return drop == null ? null : new BarricadeBuildable(k, drop);
                    }).Cast<Buildable>().Where(k => k != null));
                }

                return (owner switch
                {
                    0 when group == 0 => processedBuildables,
                    0 => processedBuildables.Where(k => k.Group == group),
                    _ => group == 0
                        ? processedBuildables.Where(k => k.Owner == owner)
                        : processedBuildables.Where(k => k.Owner == owner || k.Group == group)
                }).ToList();
            }

            var barricadeRegions = BarricadeManager.regions.Cast<BarricadeRegion>();

            if (includePlants)
                barricadeRegions = barricadeRegions.Concat(BarricadeManager.vehicleRegions);

            var structureRegions = StructureManager.regions.Cast<StructureRegion>();

            // ReSharper disable PossibleMultipleEnumeration
            var barricadeDatas = barricadeRegions.SelectMany(brd => brd.barricades).ToList();
            var barricadeDrops = barricadeRegions.SelectMany(brd => brd.drops).ToList();
            var structureDatas = structureRegions.SelectMany(str => str.structures).ToList();
            var structureDrops = structureRegions.SelectMany(str => str.drops).ToList();
            // ReSharper restore PossibleMultipleEnumeration

            var buildables = barricadeDatas
                .Select((k, i) =>
                {
                    var drop = barricadeDrops.ElementAt(i);
                    return drop == null ? null : new BarricadeBuildable(k, drop);
                }).Cast<Buildable>()
                .Concat(structureDatas.Select((k, i) =>
                {
                    var drop = structureDrops.ElementAt(i);
                    return drop == null ? null : new StructureBuildable(k, drop);
                }))
                .Where(d => d != null);

            return (owner switch
            {
                0 when group == 0 => buildables,
                0 => buildables.Where(k => k.Group == group),
                _ => group == 0
                    ? buildables.Where(k => k.Owner == owner)
                    : buildables.Where(k => k.Owner == owner || k.Group == group)
            }).ToList();
        }

        [CanBeNull]
        public static Buildable GetBuildable(Transform buildable)
        {
            return GetBuildables(includePlants: true).FirstOrDefault(k => k.Model == buildable);
        }

        [CanBeNull]
        public static Buildable GetBuildable(uint instanceId, bool isStructure)
        {
            var buildables = GetBuildables(includePlants: true);

            if (isStructure)
                buildables = buildables.OfType<StructureBuildable>();
            else
                buildables = buildables.OfType<BarricadeBuildable>();

            return buildables.FirstOrDefault(k => k.InstanceId == instanceId);
        }
    }
}