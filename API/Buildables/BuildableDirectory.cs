﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Timers;
using Pustalorc.Plugins.BaseClustering.API.Delegates;
using Pustalorc.Plugins.BaseClustering.API.Patches;
using Pustalorc.Plugins.BaseClustering.Config;
using Rocket.Core.Utils;
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

        private readonly Dictionary<uint, BarricadeBuildable> m_BarricadeBuildables;
        private readonly Dictionary<uint, StructureBuildable> m_StructureBuildables;

        private readonly HashSet<KeyValuePair<uint, bool>> m_BuildablesToRemove;
        private readonly BackgroundWorker m_BackgroundWorker;
        private readonly Timer m_WorkerTimeout;
        private readonly AutoResetEvent m_BackgroundReset;

        /// <summary>
        /// Gets a copied <see cref="IReadOnlyCollection{Buildable}"/> of all the buildables tracked.
        /// </summary>
        public IReadOnlyCollection<Buildable> Buildables => new ReadOnlyCollection<Buildable>(m_BarricadeBuildables.Values.Concat<Buildable>(m_StructureBuildables.Values).ToList());

        public BuildableDirectory(BaseClusteringPluginConfiguration configuration)
        {
            m_BarricadeBuildables = new Dictionary<uint, BarricadeBuildable>(configuration.BuildableCapacity);
            m_StructureBuildables = new Dictionary<uint, StructureBuildable>(configuration.BuildableCapacity);
            m_BackgroundWorker = new BackgroundWorker();
            m_BackgroundWorker.DoWork += HandleDestroyedInBulk;
            m_WorkerTimeout = new Timer(1000);
            m_WorkerTimeout.Elapsed += HandleElapsed;
            m_BuildablesToRemove = new HashSet<KeyValuePair<uint, bool>>();
            m_BackgroundReset = new AutoResetEvent(true);
            _instance = this;

            StructureManager.onStructureSpawned -= StructureSpawned;
            BarricadeManager.onBarricadeSpawned -= BarricadeSpawned;
            PatchBuildablesDestroy.OnBuildableDestroyed += BuildableDestroyed;
        }

        internal void LevelLoaded()
        {
            var builds = GetBuildables(useGeneratedBuilds: false);

            foreach (var element in builds)
            {
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
        }

        internal void Unload()
        {
            StructureManager.onStructureSpawned -= StructureSpawned;
            BarricadeManager.onBarricadeSpawned -= BarricadeSpawned;
            PatchBuildablesDestroy.OnBuildableDestroyed -= BuildableDestroyed;
        }

        private void HandleElapsed(object sender, ElapsedEventArgs e)
        {
            var lastAdd = m_LastAdd;
            if ((DateTime.UtcNow - lastAdd).TotalMilliseconds <= 1000)
                return;

            m_BackgroundWorker.RunWorkerAsync();

            if (lastAdd == m_LastAdd)
                m_WorkerTimeout.Stop();
        }

        private void HandleDestroyedInBulk(object sender, DoWorkEventArgs e)
        {
            m_BackgroundReset.Reset();
            TaskDispatcher.QueueOnMainThread(InternalHandleDestroyedInBulk);
        }

        private void InternalHandleDestroyedInBulk()
        {
            var affected = new List<Buildable>();

            foreach (var element in m_BuildablesToRemove)
            {
                Buildable build;

                if (element.Value)
                {
                    if (!m_StructureBuildables.TryGetValue(element.Key, out var s))
                        continue;

                    build = s;
                    m_StructureBuildables.Remove(element.Key);
                }
                else
                {
                    if (!m_BarricadeBuildables.TryGetValue(element.Key, out var b))
                        continue;

                    build = b;
                    m_BarricadeBuildables.Remove(element.Key);
                }

                affected.Add(build);
            }

            if (affected.Count > 0)
                OnBuildablesRemoved?.Invoke(affected);

            m_BackgroundReset.Set();
        }

        internal void WaitDestroyHandle()
        {
            if (m_WorkerTimeout.Enabled || m_BuildablesToRemove.Count > 0 || m_BackgroundWorker.IsBusy)
                m_BackgroundReset.WaitOne();

            if (m_BuildablesToRemove.Count > 0)
                InternalHandleDestroyedInBulk();
        }

        private DateTime m_LastAdd;

        private void BuildableDestroyed(uint instanceId, bool isStructure)
        {
            m_LastAdd = DateTime.UtcNow;
            m_BuildablesToRemove.Add(new KeyValuePair<uint, bool>(instanceId, isStructure));

            if (!m_WorkerTimeout.Enabled)
                m_WorkerTimeout.Start();
        }

        private void StructureSpawned(StructureRegion region, StructureDrop drop)
        {
            var data = region.structures.Find(k => k.instanceID == drop.instanceID);
            BuildableSpawned(new StructureBuildable(data, drop));
        }

        private void BarricadeSpawned(BarricadeRegion region, BarricadeDrop drop)
        {
            var data = region.barricades.Find(k => k.instanceID == drop.instanceID);
            BuildableSpawned(new BarricadeBuildable(data, drop));
        }

        private void BuildableSpawned(Buildable buildable)
        {
            switch (buildable)
            {
                case BarricadeBuildable b:
                    m_BarricadeBuildables.Add(buildable.InstanceId, b);
                    break;
                case StructureBuildable s:
                    m_StructureBuildables.Add(buildable.InstanceId, s);
                    break;
                default:
                    return;
            }

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