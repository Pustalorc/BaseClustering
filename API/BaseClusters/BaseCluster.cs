using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Buildables;
using Pustalorc.Plugins.BaseClustering.API.Delegates;
using Pustalorc.Plugins.BaseClustering.API.Utilities;
using Pustalorc.Plugins.BaseClustering.Config;
using SDG.Unturned;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Pustalorc.Plugins.BaseClustering.API.BaseClusters
{
    /// <summary>
    /// This class defines the structure of a BaseCluster.
    /// </summary>
    public sealed class BaseCluster
    {
        private readonly BaseClusteringPluginConfiguration m_PluginConfiguration;
        private readonly BaseClusterDirectory m_BaseClusterDirectory;
        private readonly List<Buildable> m_Buildables;

        /// <summary>
        /// This event is raised if <see cref="Reset"/> is called.
        /// </summary>
        [UsedImplicitly]
        public event VoidDelegate? OnClusterReset;

        /// <summary>
        /// This event is raised if new buildables are added to the cluster.
        /// </summary>
        [UsedImplicitly]
        public event BuildablesChanged? OnBuildablesAdded;

        /// <summary>
        /// This event is raised if buildables are removed from the cluster.
        /// </summary>
        [UsedImplicitly]
        public event BuildablesChanged? OnBuildablesRemoved;

        /// <summary>
        /// Gets the common owner of the entire cluster based on who owns the most things.
        /// </summary>
        public ulong CommonOwner => m_Buildables.GroupBy(k => k.Owner)
            .OrderByDescending(k => k.Count())
            .Select(g => g.Key).ToList().FirstOrDefault();

        /// <summary>
        /// Gets the common group of the entire cluster based on which group owns the most things.
        /// </summary>
        [UsedImplicitly]
        public ulong CommonGroup => m_Buildables.GroupBy(k => k.Group)
            .OrderByDescending(k => k.Count())
            .Select(g => g.Key).ToList().FirstOrDefault();

        /// <summary>
        /// Gets the average center position of the cluster.
        /// </summary>
        public Vector3 AverageCenterPosition =>
            m_Buildables.OfType<StructureBuildable>().AverageCenter(k => k.Position);

        /// <summary>
        /// Gets the unique instanceId of this cluster.
        /// </summary>
        public int InstanceId { get; }

        /// <summary>
        /// Defines if this cluster is a global cluster.
        /// </summary>
        public bool IsGlobalCluster { get; }

        /// <summary>
        /// Defines if this cluster is being destroyed, and therefore integrity check operations shouldn't be handled.
        /// </summary>
        public bool IsBeingDestroyed { get; set; }

        /// <summary>
        /// Gets a copied <see cref="IReadOnlyCollection{Buildable}"/> of all the buildables in the cluster.
        /// </summary>
        public IReadOnlyCollection<Buildable> Buildables => new ReadOnlyCollection<Buildable>(m_Buildables.ToList());

        internal BaseCluster(BaseClusteringPluginConfiguration pluginConfiguration,
            BaseClusterDirectory baseClusterDirectory, int instanceId, bool isGlobalCluster = false)
        {
            m_PluginConfiguration = pluginConfiguration;
            m_BaseClusterDirectory = baseClusterDirectory;
            m_Buildables = new List<Buildable>();
            InstanceId = instanceId;
            IsGlobalCluster = isGlobalCluster;
        }

        /// <summary>
        /// Destroys this base, including all the buildables in it.
        /// </summary>
        /// <param name="shouldDropItems">
        /// If <see langword="true"/>, <see cref="InteractableStorage"/>s will drop all their contents on the ground.
        /// <br/>
        /// If <see langword="false"/>, <see cref="InteractableStorage"/>s will not drop any of their contents on the ground.
        /// </param>
        public void Destroy(bool shouldDropItems = true)
        {
            IsBeingDestroyed = true;

            foreach (var buildable in Buildables.ToList())
            {
                if (buildable.Interactable is InteractableStorage store)
                    store.despawnWhenDestroyed = !shouldDropItems;

                buildable.SafeDestroy();
            }

            m_BaseClusterDirectory.Return(this);

            if (IsGlobalCluster)
                IsBeingDestroyed = false;
        }

        /// <summary>
        /// Checks if another buildable is within range of this base cluster.
        /// </summary>
        /// <param name="buildable">The buildable to check.</param>
        /// <returns>
        /// <see langword="true"/> if the buildable is within range.
        /// <br/>
        /// <see langword="false"/> if the buildable is outside range.
        /// </returns>
        public bool IsWithinRange(Buildable buildable)
        {
            var structures = Buildables.OfType<StructureBuildable>();
            var distanceCheck = buildable is StructureBuildable
                ? Mathf.Pow(m_PluginConfiguration.MaxDistanceBetweenStructures, 2)
                : Mathf.Pow(m_PluginConfiguration.MaxDistanceToConsiderPartOfBase, 2);

            return structures.Any(k => (k.Position - buildable.Position).sqrMagnitude <= distanceCheck);
        }

        /// <summary>
        /// Checks if a position is within range of this base cluster.
        /// </summary>
        /// <param name="position">The position to check.</param>
        /// <returns>
        /// <see langword="true"/> If the position is within range.
        /// <br/>
        /// <see langword="false"/> If the position is outside range.
        /// </returns>
        /// <remarks>
        /// Unlike <see cref="IsWithinRange(Buildable)"/>, this method only checks with <see cref="BaseClusteringPluginConfiguration.MaxDistanceToConsiderPartOfBase"/> not with <see cref="BaseClusteringPluginConfiguration.MaxDistanceBetweenStructures"/>.
        /// </remarks>
        public bool IsWithinRange(Vector3 position)
        {
            var distanceCheck = Mathf.Pow(m_PluginConfiguration.MaxDistanceToConsiderPartOfBase, 2);

            return Buildables.OfType<StructureBuildable>().Any(k => (k.Position - position).sqrMagnitude <= distanceCheck);
        }

        /// <summary>
        /// Resets a base to the default state (empty).
        /// <br/>
        /// This method only clears the buildables that are in it and raises <see cref="OnClusterReset"/>.
        /// </summary>
        public void Reset()
        {
            m_Buildables.Clear();
            OnClusterReset?.Invoke();
        }

        /// <summary>
        /// Adds a buildable to the base. This method does not spawn in a buildable.
        /// </summary>
        /// <param name="build">The buildable to add to the base.</param>
        public void AddBuildable(Buildable build)
        {
            var isStruct = build is StructureBuildable;
            if (IsGlobalCluster && isStruct)
                throw new NotSupportedException("StructureBuildables are not supported by global clusters.");

            m_Buildables.Add(build);
            var gCluster = m_BaseClusterDirectory.GetOrCreateGlobalCluster();
            var buildsInRange = gCluster.Buildables.Where(IsWithinRange).ToList();
            AddBuildables(buildsInRange);
            gCluster.RemoveBuildables(buildsInRange);
            // Include the buildables from the global cluster that got added.
            OnBuildablesAdded?.Invoke(buildsInRange.Concat(new[] {build}));
        }

        /// <summary>
        /// Adds multiple buildables to the base. This method does not spawn in any of the buildables.
        /// </summary>
        /// <param name="builds">The <see cref="IEnumerable{Buildable}"/> of <see cref="Buildable"/>s to add to the base.</param>
        public void AddBuildables(IEnumerable<Buildable> builds)
        {
            var consolidate = builds.ToList();
            m_Buildables.AddRange(consolidate);
            OnBuildablesAdded?.Invoke(consolidate);
        }

        /// <summary>
        /// Removes a buildable from the base. This method does not destroy the buildable.
        /// </summary>
        /// <param name="build">The buildable to remove from the base.</param>
        [UsedImplicitly]
        public void RemoveBuildable(Buildable build)
        {
            var removedSomething = m_Buildables.Remove(build);
            if (removedSomething)
                OnBuildablesRemoved?.Invoke(new[] {build});

            if (removedSomething && !IsBeingDestroyed && !IsGlobalCluster)
                VerifyAndCorrectIntegrity();
        }

        /// <summary>
        /// Removes multiple buildables from the base. This method does not destroy any of the buildables.
        /// </summary>
        /// <param name="builds">The <see cref="IEnumerable{Buildable}"/> of <see cref="Buildable"/>s to remove from the base.</param>
        public void RemoveBuildables(List<Buildable> builds)
        {
            var removed = new List<Buildable>();

            foreach (var build in m_Buildables.Where(builds.Remove).ToList())
            {
                m_Buildables.Remove(build);
                removed.Add(build);
            }

            if (removed.Count > 0)
                OnBuildablesRemoved?.Invoke(removed);

            if (removed.Count > 0 && !IsBeingDestroyed && !IsGlobalCluster)
                VerifyAndCorrectIntegrity();
        }

        private bool VerifyStructureIntegrity()
        {
            var allStructures = Buildables.OfType<StructureBuildable>().ToList();

            if (allStructures.Count <= 0)
                return false;

            var maxStructureDistance = Mathf.Pow(m_PluginConfiguration.MaxDistanceBetweenStructures, 2);
            var succeeded = new List<StructureBuildable>();

            var random = allStructures[Random.Range(0, allStructures.Count)];
            succeeded.Add(random);
            allStructures.Remove(random);

            for (var i = 0; i < succeeded.Count; i++)
            {
                var element = succeeded[i];

                var result = allStructures
                    .Where(k => (element.Position - k.Position).sqrMagnitude <= maxStructureDistance)
                    .ToList();
                succeeded.AddRange(result);
                allStructures.RemoveAll(result.Contains);
            }

            return allStructures.Count == 0;
        }

        private bool VerifyBarricadeIntegrity()
        {
            var structures = Buildables.OfType<StructureBuildable>().ToList();

            if (structures.Count <= 0)
                return false;

            var maxBuildableDistance =
                Mathf.Pow(m_PluginConfiguration.MaxDistanceToConsiderPartOfBase, 2);

            return Buildables.OfType<BarricadeBuildable>().All(br =>
                structures.Exists(k => (br.Position - k.Position).sqrMagnitude <= maxBuildableDistance));
        }

        /// <summary>
        /// This will verify the base integrity (that all the elements are still within range of configured limits) and if not, it will correct that.
        /// </summary>
        private void VerifyAndCorrectIntegrity()
        {
            var structureIntegrity = VerifyStructureIntegrity();
            var barricadeIntegrity = VerifyBarricadeIntegrity();

            // If the base is still integrally sound, skip the rest of the code
            if (structureIntegrity && barricadeIntegrity) return;

            var globalCluster = m_BaseClusterDirectory.GetOrCreateGlobalCluster();

            IsBeingDestroyed = true;
            // If the structure is still integral, check the barricades and fix any non-integral parts.
            if (structureIntegrity)
            {
                // Get all the barricades that are too far from the cluster in a copied list.
                foreach (var b in Buildables.OfType<BarricadeBuildable>().Where(k => !IsWithinRange(k)).ToList())
                {
                    // Find the next best cluster that this element is within
                    var bestCluster = m_BaseClusterDirectory.FindBestCluster(b);

                    // If something is found, check that its not the same cluster we are already in.
                    if (bestCluster != null)
                    {
                        if (bestCluster != this)
                        {
                            // If its a different cluster, remove it from the current cluster and add it to the new one.
                            RemoveBuildable(b);
                            bestCluster.AddBuildable(b);
                        }

                        continue;
                    }

                    // If no best cluster is found, check if we have a global cluster. If we do, add the barricade to it. If we don't, create a new global cluster.
                    RemoveBuildable(b);
                    globalCluster.AddBuildable(b);
                }

                IsBeingDestroyed = false;
                return;
            }

            // First, get a list of all buildables to cluster, including global cluster.
            var builds = Buildables.Concat(globalCluster.Buildables).ToList();
            globalCluster.Reset();
            var clusterRegened = m_BaseClusterDirectory.ClusterElements(builds)
                .OrderByDescending(k => k.Buildables.Count).ToList();

            // Dispose correctly of the cluster we are not going to add here.
            var discarded = clusterRegened.FirstOrDefault();
            m_BaseClusterDirectory.Return(discarded);

            // Select all the clusters, except for the largest one.
            foreach (var c in clusterRegened.Skip(1).ToList())
            {
                // Remove any of the elements on the new cluster from the old one.
                RemoveBuildables(c.Buildables.ToList());

                // Add the new cluster to the directory.
                m_BaseClusterDirectory.RegisterCluster(c);
            }

            // Finally, if there's no structure buildables left in this cluster, call to remove it.
            if (!Buildables.OfType<StructureBuildable>().Any())
                m_BaseClusterDirectory.Return(this);

            IsBeingDestroyed = false;
        }

        internal void StealFromGlobal(BaseCluster? globalCluster)
        {
            if (globalCluster?.IsGlobalCluster != true)
                return;

            foreach (var build in globalCluster.Buildables.Where(IsWithinRange))
            {
                AddBuildable(build);
                globalCluster.RemoveBuildable(build);
            }
        }
    }
}