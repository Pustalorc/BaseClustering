using System;
using System.Collections.Generic;
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
    public sealed class BaseCluster
    {
        private readonly BaseClusteringPluginConfiguration m_PluginConfiguration;
        private readonly BaseClusterDirectory m_BaseClusterDirectory;
        private readonly List<Buildable> m_Buildables;

        [UsedImplicitly] public event VoidDelegate OnClusterReset;
        [UsedImplicitly] public event BuildablesChanged OnBuildablesAdded;
        [UsedImplicitly] public event BuildablesChanged OnBuildablesRemoved;

        public ulong CommonOwner => m_Buildables.GroupBy(k => k.Owner)
            .OrderByDescending(k => k.Count())
            .Select(g => g.Key).ToList().FirstOrDefault();

        public ulong CommonGroup => m_Buildables.GroupBy(k => k.Group)
            .OrderByDescending(k => k.Count())
            .Select(g => g.Key).ToList().FirstOrDefault();

        public Vector3 AverageCenterPosition =>
            m_Buildables.OfType<StructureBuildable>().AverageCenter(k => k.Position);

        public int InstanceId { get; }

        public bool IsGlobalCluster { get; }

        public bool IsBeingDestroyed { get; set; }

        public IReadOnlyCollection<Buildable> Buildables => m_Buildables;

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
        /// Destroy this base, including all the buildables in it.
        /// </summary>
        /// <param name="shouldDropItems">Should items from storages in the base drop?</param>
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
        }

        /// <summary>
        /// Check if another buildable is within range of the base.
        /// </summary>
        /// <param name="buildable">The buildable to check.</param>
        /// <returns>If the buildable provided is within range of the base.</returns>
        public bool IsWithinRange(Buildable buildable)
        {
            var structures = Buildables.OfType<StructureBuildable>();
            var distanceCheck = buildable is StructureBuildable
                ? Mathf.Pow(m_PluginConfiguration.MaxDistanceBetweenStructures, 2)
                : Mathf.Pow(m_PluginConfiguration.MaxDistanceToConsiderPartOfBase, 2);

            return structures.Any(k => (k.Position - buildable.Position).sqrMagnitude <= distanceCheck);
        }

        /// <summary>
        /// Check if a position is within range of the base.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <returns>If the position provided is within range of the base</returns>
        public bool IsWithinRange(Vector3 position)
        {
            var distanceCheck = Mathf.Pow(m_PluginConfiguration.MaxDistanceToConsiderPartOfBase, 2);

            return Buildables.Any(k => (k.Position - position).sqrMagnitude <= distanceCheck);
        }

        /// <summary>
        /// Resets a base to the default. This method currently only clears the buildables that are in it.
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
            var buildsInRange = gCluster.Buildables.Where(IsWithinRange);
            AddBuildables(buildsInRange);
            gCluster.RemoveBuildables(buildsInRange);
            OnBuildablesAdded?.Invoke(new[] {build});
        }

        /// <summary>
        /// Adds multiple buildables to the base. This method does not spawn in any of the buildables.
        /// </summary>
        /// <param name="builds">The IEnumerable of buildables to add to the base.</param>
        public void AddBuildables([NotNull] IEnumerable<Buildable> builds)
        {
            var consolidate = builds.ToList();
            m_Buildables.AddRange(consolidate);
            OnBuildablesAdded?.Invoke(consolidate);
        }

        /// <summary>
        /// Removes a buildable from the base. This method does not destroy the buildable.
        /// </summary>
        /// <param name="build">The buildable to remove from the base.</param>
        public void RemoveBuildable(Buildable build)
        {
            var builds = new[] {build};
            if (m_Buildables.Remove(build))
                OnBuildablesRemoved?.Invoke(builds);

            if (!IsBeingDestroyed && !IsGlobalCluster)
                VerifyAndCorrectIntegrity(builds);
        }

        /// <summary>
        /// Removes multiple buildables from the base. This method does not destroy any of the buildables.
        /// </summary>
        /// <param name="builds">The buildables to remove from the base.</param>
        public void RemoveBuildables([NotNull] IEnumerable<Buildable> builds)
        {
            var buildables = builds.ToList();

            var removed = m_Buildables.Where(buildables.Contains).ToList();
            m_Buildables.RemoveAll(removed.Contains);

            if (removed.Count > 0)
                OnBuildablesRemoved?.Invoke(removed);

            if (!IsBeingDestroyed && !IsGlobalCluster)
                VerifyAndCorrectIntegrity(buildables);
        }

        private bool VerifyStructureIntegrityForced([NotNull] IList<StructureBuildable> structures)
        {
            var maxStructureDistance = Mathf.Pow(m_PluginConfiguration.MaxDistanceBetweenStructures, 2);
            var succeeded = new List<StructureBuildable>();
            var result = true;

            var random = structures[Random.Range(0, structures.Count)];
            random = structures.OrderByDescending(k => (random.Position - k.Position).sqrMagnitude).First();
            succeeded.Add(random);
            structures.Remove(random);

            foreach (var str in structures.OrderBy(k => (random.Position - k.Position).sqrMagnitude))
            {
                if (!succeeded.Exists(k => (str.Position - k.Position).sqrMagnitude <= maxStructureDistance))
                {
                    result = false;
                    break;
                }

                succeeded.Add(str);
            }

            return result;
        }

        private bool VerifyStructureIntegrity([CanBeNull] Buildable removedBuildable = null)
        {
            var allStructures = Buildables.OfType<StructureBuildable>().ToList();

            if (allStructures.Count <= 0)
                return false;

            var affectedArea = new List<StructureBuildable>();

            var result = true;

            if (removedBuildable != null)
            {
                var maxDist = Mathf.Pow(m_PluginConfiguration.DestroyIntegrityCheckDistance, 2);
                affectedArea = allStructures
                    .Where(k => (removedBuildable.Position - k.Position).sqrMagnitude <= maxDist).ToList();
                result = VerifyStructureIntegrityForced(affectedArea);
            }

            if (!result && affectedArea.Count != allStructures.Count)
                result = VerifyStructureIntegrityForced(allStructures);

            return result;
        }

        private bool VerifyBarricadeIntegrity()
        {
            var barricades = Buildables.OfType<BarricadeBuildable>().ToList();
            var structures = Buildables.OfType<StructureBuildable>().ToList();
            var maxBuildableDistance =
                Mathf.Pow(m_PluginConfiguration.MaxDistanceToConsiderPartOfBase, 2);

            return barricades.All(br =>
                structures.Exists(k => (br.Position - k.Position).sqrMagnitude <= maxBuildableDistance));
        }

        // WARNING: SERVER FREEZES HERE!!!!
        private void VerifyAndCorrectIntegrity([NotNull] IEnumerable<Buildable> removedBuildables)
        {
            var structureIntegrity = !removedBuildables.OfType<StructureBuildable>().Any(k => !VerifyStructureIntegrity(k));
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
                            m_Buildables.Remove(b);
                            bestCluster.AddBuildable(b);
                        }

                        continue;
                    }

                    // If no best cluster is found, check if we have a global cluster. If we do, add the barricade to it. If we don't, create a new global cluster.
                    globalCluster.AddBuildable(b);
                    m_Buildables.Remove(b);
                }

                IsBeingDestroyed = false;
                return;
            }

            // First, ask the clustering tool to generate a new set of clusters from the current buildables.
            var clusterRegened = m_BaseClusterDirectory.ClusterElements(Buildables.Concat(globalCluster.Buildables).ToList());
            globalCluster.Reset();

            // Select all the clusters, except for the largest one.
            foreach (var c in clusterRegened.OrderByDescending(k => k.Buildables.Count).Skip(1).ToList())
            {
                // Remove any of the elements on the new cluster from the old one.
                RemoveBuildables(c.Buildables);

                // Add the new cluster to the directory.
                m_BaseClusterDirectory.RegisterCluster(c);
            }

            // Finally, if there's no structure buildables left int his cluster, call to remove it.
            if (!Buildables.OfType<StructureBuildable>().Any())
                m_BaseClusterDirectory.Return(this);

            IsBeingDestroyed = false;
        }
    }
}