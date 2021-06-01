using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Timers;
using Pustalorc.Plugins.BaseClustering.API.Delegates;
using Pustalorc.Plugins.BaseClustering.API.Patches;
using SDG.Unturned;
using UnityEngine;
using Timer = System.Timers.Timer;

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
        public event BuildableChange? OnBuildableAdded;

        /// <summary>
        /// This event is raised every time buildables are removed (in bulk).
        /// </summary>
        public event BuildablesChanged? OnBuildablesRemoved;

        private readonly List<Buildable> m_Buildables;
        private readonly List<Transform> m_TargetBuildsToRemove;
        private readonly BackgroundWorker m_BackgroundWorker;
        private readonly Timer m_WorkerTimeout;
        private readonly AutoResetEvent m_BackgroundReset;

        /// <summary>
        /// Gets a copied <see cref="IReadOnlyCollection{Buildable}"/> of all the buildables tracked.
        /// </summary>
        public IReadOnlyCollection<Buildable> Buildables
        {
            get
            {
                var cloned = new List<Buildable>();

                // Disabled warning as we want a thread-safe copy. Using .ToList() results in the code using a foreach,
                // so if another thread .Removes or .Adds during that .ToList(), the code will throw ModifiedCollection exception.
                // ReSharper disable once LoopCanBeConvertedToQuery
                // ReSharper disable once ForCanBeConvertedToForeach
                for (var i = 0; i < m_Buildables.Count; i++)
                    cloned.Add(m_Buildables[i]);

                return new ReadOnlyCollection<Buildable>(cloned);
            }
        }

        public BuildableDirectory()
        {
            m_Buildables = new List<Buildable>();
            m_BackgroundWorker = new BackgroundWorker();
            m_BackgroundWorker.DoWork += HandleDestroyedInBulk;
            m_WorkerTimeout = new Timer(1000);
            m_WorkerTimeout.Elapsed += HandleElapsed;
            m_TargetBuildsToRemove = new List<Transform>();
            m_BackgroundReset = new AutoResetEvent(true);
            _instance = this;

            PatchBuildableSpawns.OnBuildableSpawned += BuildableSpawned;
            PatchBuildablesDestroy.OnBuildableDestroyed += BuildableDestroyed;
        }

        internal void LevelLoaded()
        {
            var builds = GetBuildables(useGeneratedBuilds: false);
            m_Buildables.AddRange(builds);
        }

        private void HandleElapsed(object sender, ElapsedEventArgs e)
        {
            var lastAdd = m_LastAdd;
            if ((DateTime.UtcNow - lastAdd).TotalMilliseconds <= 1000)
                return;

            m_BackgroundWorker.RunWorkerAsync();

            if (lastAdd == m_LastAdd && m_TargetBuildsToRemove.Count == 0)
                m_WorkerTimeout.Stop();
        }

        private void HandleDestroyedInBulk(object? sender, DoWorkEventArgs? e)
        {
            m_BackgroundReset.Reset();

            var affected = new List<Buildable>();
            var clonedTargets = new List<Transform>();

            for (var i = m_TargetBuildsToRemove.Count - 1; i >= 0; i--)
            {
                clonedTargets.Add(m_TargetBuildsToRemove[i]);
                m_TargetBuildsToRemove.RemoveAt(i);
            }

            for (var i = m_Buildables.Count - 1; i >= 0; i--)
            {
                var build = m_Buildables[i];

                if (!clonedTargets.Remove(build.Model))
                    continue;

                m_Buildables.Remove(build);
                affected.Add(build);
            }

            if (affected.Count > 0)
                OnBuildablesRemoved?.Invoke(affected);

            m_BackgroundReset.Set();
        }

        internal void Unload()
        {
            PatchBuildableSpawns.OnBuildableSpawned -= BuildableSpawned;
            PatchBuildablesDestroy.OnBuildableDestroyed -= BuildableDestroyed;
        }

        private DateTime m_LastAdd;

        private void BuildableDestroyed(Transform buildable)
        {
            m_LastAdd = DateTime.UtcNow;
            m_TargetBuildsToRemove.Add(buildable);

            if (!m_WorkerTimeout.Enabled)
                m_WorkerTimeout.Start();
        }

        private void BuildableSpawned(Buildable buildable)
        {
            m_Buildables.Add(buildable);
            OnBuildableAdded?.Invoke(buildable);
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
        public static IEnumerable<Buildable> GetBuildables(ulong owner = 0, ulong group = 0, bool includePlants = false, bool useGeneratedBuilds = true)
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

        internal void WaitDestroyHandle()
        {
            if (m_WorkerTimeout.Enabled || m_TargetBuildsToRemove.Count > 0 || m_BackgroundWorker.IsBusy)
                m_BackgroundReset.WaitOne();

            if (m_TargetBuildsToRemove.Count > 0)
                HandleDestroyedInBulk(null, null);
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