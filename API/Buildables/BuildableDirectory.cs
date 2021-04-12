using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Delegates;
using Pustalorc.Plugins.BaseClustering.API.Patches;
using SDG.Unturned;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.API.Buildables
{
    public sealed class BuildableDirectory
    {
        private static BuildableDirectory _instance;

        public event BuildableChange OnBuildableAdded;
        public event BuildableChange OnBuildableRemoved;

        private readonly List<Buildable> m_Buildables;

        [NotNull] public IReadOnlyCollection<Buildable> Buildables => m_Buildables.AsReadOnly();

        public BuildableDirectory()
        {
            var builds = GetBuildables();
            m_Buildables = builds.ToList();
            _instance = this;

            PatchBuildableSpawns.OnBuildableSpawned += BuildableSpawned;
            PatchBuildablesDestroy.OnBuildableDestroyed += BuildableDestroyed;
        }

        internal void Unload()
        {
            PatchBuildableSpawns.OnBuildableSpawned -= BuildableSpawned;
            PatchBuildablesDestroy.OnBuildableDestroyed -= BuildableDestroyed;
        }

        private void BuildableDestroyed(Buildable buildable)
        {
            if (m_Buildables.Remove(buildable))
                OnBuildableRemoved?.Invoke(buildable);
        }

        private void BuildableSpawned(Buildable buildable)
        {
            m_Buildables.Add(buildable);
            OnBuildableAdded?.Invoke(buildable);
        }

        [NotNull]
        public static IEnumerable<Buildable> GetBuildables(ulong owner = 0, ulong group = 0, bool includePlants = false,
            bool useGeneratedBuilds = true)
        {
            IEnumerable<Buildable> result;

            if (useGeneratedBuilds && _instance != null)
            {
                result = _instance.Buildables;

                if (!includePlants)
                    result = result.Except(result.Where(k => k.IsPlanted));
            }
            else
            {
                var barricadeRegions = BarricadeManager.regions.Cast<BarricadeRegion>().ToList();

                if (includePlants)
                    barricadeRegions.AddRange(BarricadeManager.vehicleRegions);

                var structureRegions = StructureManager.regions.Cast<StructureRegion>().ToList();

                var barricadeDatas = barricadeRegions.SelectMany(brd => brd.barricades).ToList();
                var barricadeDrops = barricadeRegions.SelectMany(brd => brd.drops).ToList();
                var structureDatas = structureRegions.SelectMany(str => str.structures).ToList();
                var structureDrops = structureRegions.SelectMany(str => str.drops).ToList();

                result = barricadeDatas
                    .Select((k, i) =>
                    {
                        var drop = barricadeDrops.ElementAt(i);
                        return drop == null ? null : new BarricadeBuildable(k, drop);
                    })
                    .Concat<Buildable>(structureDatas.Select((k, i) =>
                    {
                        var drop = structureDrops.ElementAt(i);
                        return drop == null ? null : new StructureBuildable(k, drop);
                    }))
                    .Where(d => d != null);
            }

            return (owner switch
            {
                0 when group == 0 => result,
                0 => result.Where(k => k.Group == group),
                _ => group == 0
                    ? result.Where(k => k.Owner == owner)
                    : result.Where(k => k.Owner == owner || k.Group == group)
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