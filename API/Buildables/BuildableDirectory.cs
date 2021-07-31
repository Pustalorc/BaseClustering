using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Pustalorc.Plugins.BaseClustering.API.Delegates;
using Pustalorc.Plugins.BaseClustering.API.Patches;
using Pustalorc.Plugins.BaseClustering.Config;
using SDG.Unturned;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.API.Buildables
{
    /// <summary>
    /// A directory that keeps track of all <see cref="Buildable"/>s.
    /// </summary>
    public sealed class BuildableDirectory
    {
        /// <summary>
        /// An internal singleton accessor. To be used only by <see cref="BuildableDirectory.GetBuildables"/>.
        /// </summary>
        private static BuildableDirectory? _instance;

        /// <summary>
        /// This event is raised every time a buildable is added.
        /// </summary>
        public event BuildablesChanged? OnBuildablesAdded;

        /// <summary>
        /// This event is raised every time buildables are removed (in bulk).
        /// </summary>
        public event BuildablesChanged? OnBuildablesRemoved;

        private readonly Dictionary<uint, BarricadeBuildable> m_BarricadeBuildables;
        private readonly Dictionary<uint, StructureBuildable> m_StructureBuildables;

        private readonly ConcurrentQueue<Buildable> m_DeferredRemove;
        private readonly ConcurrentQueue<Buildable> m_DeferredAdd;

        private readonly AutoResetEvent m_BackgroundWorkerEnd;
        private readonly int m_BackgroundWorkerSleepTime;

        private BackgroundWorker m_BackgroundWorker;

        /// <summary>
        /// Gets a copied <see cref="IReadOnlyCollection{Buildable}"/> of all the buildables tracked.
        /// </summary>
        public IReadOnlyCollection<Buildable> Buildables =>
            new ReadOnlyCollection<Buildable>(m_BarricadeBuildables.Values
                .Concat<Buildable>(m_StructureBuildables.Values).ToList());

        /// <summary>
        /// Creates a new instance of the buildable directory.
        /// </summary>
        /// <param name="configuration">The plugin's configuration to utilize here.</param>
        public BuildableDirectory(BaseClusteringPluginConfiguration configuration)
        {
            m_BarricadeBuildables = new Dictionary<uint, BarricadeBuildable>(configuration.BuildableCapacity);
            m_StructureBuildables = new Dictionary<uint, StructureBuildable>(configuration.BuildableCapacity);
            m_DeferredRemove = new ConcurrentQueue<Buildable>();
            m_DeferredAdd = new ConcurrentQueue<Buildable>();
            m_BackgroundWorkerEnd = new AutoResetEvent(false);
            m_BackgroundWorkerSleepTime = configuration.BackgroundWorkerSleepTime;
            _instance = this;

            StructureManager.onStructureSpawned += StructureSpawned;
            BarricadeManager.onBarricadeSpawned += BarricadeSpawned;
            PatchBuildablesDestroy.OnBuildableDestroyed += BuildableDestroyed;

            m_BackgroundWorker = new BackgroundWorker {WorkerSupportsCancellation = true};
            RestartBackgroundWorker();
        }

        internal void LevelLoaded()
        {
            var builds = GetBuildables(useGeneratedBuilds: false);

            foreach (var element in builds)
                switch (element)
                {
                    case BarricadeBuildable b:
                        m_BarricadeBuildables.Add(element.InstanceId, b);
                        break;
                    case StructureBuildable s:
                        m_StructureBuildables.Add(element.InstanceId, s);
                        break;
                }
        }

        internal void Unload()
        {
            StructureManager.onStructureSpawned -= StructureSpawned;
            BarricadeManager.onBarricadeSpawned -= BarricadeSpawned;
            PatchBuildablesDestroy.OnBuildableDestroyed -= BuildableDestroyed;
        }

        private void HandleDeferred(object sender, DoWorkEventArgs e)
        {
            while (!m_BackgroundWorker.CancellationPending)
            {
                InternalHandleDeferred();
                Thread.Sleep(m_BackgroundWorkerSleepTime);
            }

            m_BackgroundWorkerEnd.Set();
        }

        private void InternalHandleDeferred()
        {
            var deferredAdd = new List<Buildable>();
            while (m_DeferredAdd.TryDequeue(out var element))
                deferredAdd.Add(element);

            var deferredRemove = new List<Buildable>();
            while (m_DeferredRemove.TryDequeue(out var element))
                deferredRemove.Add(element);

            OnBuildablesAdded?.Invoke(deferredAdd);
            OnBuildablesRemoved?.Invoke(deferredRemove);
        }

        internal void WaitDestroyHandle()
        {
            if (m_BackgroundWorker.IsBusy)
            {
                m_BackgroundWorker.CancelAsync();
                m_BackgroundWorkerEnd.WaitOne();
            }

            InternalHandleDeferred();
        }

        internal void RestartBackgroundWorker()
        {
            m_BackgroundWorker = new BackgroundWorker {WorkerSupportsCancellation = true};
            m_BackgroundWorker.DoWork += HandleDeferred;
            m_BackgroundWorker.RunWorkerAsync();
        }

        private void BuildableDestroyed(uint instanceId, bool isStructure)
        {
            Buildable? build;
            bool removed;

            switch (isStructure)
            {
                case true when m_StructureBuildables.TryGetValue(instanceId, out var s):
                    build = s;
                    removed = m_StructureBuildables.Remove(instanceId);
                    break;
                case false when m_BarricadeBuildables.TryGetValue(instanceId, out var b):
                    build = b;
                    removed = m_BarricadeBuildables.Remove(instanceId);
                    break;
                default:
                    return;
            }

            if (!removed || build == null)
                return;

            m_DeferredRemove.Enqueue(build);
        }

        private void StructureSpawned(StructureRegion region, StructureDrop drop)
        {
            var data = region.structures.Find(k => k.instanceID == drop.instanceID);
            var build = new StructureBuildable(data, drop);
            m_StructureBuildables.Add(build.InstanceId, build);
            m_DeferredAdd.Enqueue(build);
        }

        private void BarricadeSpawned(BarricadeRegion region, BarricadeDrop drop)
        {
            var data = region.barricades.Find(k => k.instanceID == drop.instanceID);
            var build = new BarricadeBuildable(data, drop);
            m_BarricadeBuildables.Add(build.InstanceId, build);
            m_DeferredAdd.Enqueue(build);
        }

        /// <summary>
        /// Gets all of the <see cref="Buildable"/>s from the map or from the already generated cache.
        /// </summary>
        /// <param name="owner">The owner with which to filter the result.</param>
        /// <param name="group">The group with which to filter the result.</param>
        /// <param name="includePlants">If planted (on vehicle) barricades should be included.</param>
        /// <param name="useGeneratedBuilds">If the <see cref="Buildables"/> should be used instead of generating a new <see cref="IEnumerable{Buildable}"/> from the map.</param>
        /// <returns>An <see cref="IEnumerable{Buildable}"/></returns>
        /// <remarks>
        /// If <paramref name="owner"/> or <paramref name="group"/> are equal to 0, then there will be no filtering done respective to whichever is 0.
        /// </remarks>
        public static IEnumerable<Buildable> GetBuildables(ulong owner = 0, ulong group = 0, bool includePlants = false,
            bool useGeneratedBuilds = true)
        {
            IEnumerable<Buildable> result;

            if (useGeneratedBuilds && _instance != null)
            {
                result = _instance.Buildables;
                if (!includePlants)
                    result = result.Where(k => !k.IsPlanted);
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
                    .Concat<Buildable?>(structureDatas.Select((k, i) =>
                    {
                        var drop = structureDrops.ElementAt(i);
                        return drop == null ? null : new StructureBuildable(k, drop);
                    }))
                    .Where(d => d != null)!;
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

        /// <summary>
        /// Gets a specific buildable based on a <see cref="Transform"/>.
        /// </summary>
        /// <param name="buildable">The <see cref="Transform"/> of the buildable to find.</param>
        /// <returns>
        /// <see langword="null"/> if the buildable was not found.
        /// <br/>
        /// An instance of <see cref="Buildable"/> if the buildable was found.
        /// </returns>
        public static Buildable? GetBuildable(Transform buildable)
        {
            return GetBuildables(includePlants: true).FirstOrDefault(k => k.Model == buildable);
        }

        /// <summary>
        /// Gets a specific buildable based on their instanceId and if they are a structure or not.
        /// </summary>
        /// <param name="instanceId">The instance id of the buildable to find.</param>
        /// <param name="isStructure">If the buildable we are trying to find is a structure or a barricade.</param>
        /// <returns>
        /// <see langword="null"/> if the buildable was not found.
        /// <br/>
        /// An instance of <see cref="Buildable"/> if the buildable was found.
        /// </returns>
        public static Buildable? GetBuildable(uint instanceId, bool isStructure)
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