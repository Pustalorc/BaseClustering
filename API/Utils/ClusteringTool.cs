using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Buildables;
using SDG.Unturned;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.API.Utils
{
    public sealed class ClusteringTool
    {
        private readonly BaseClusteringPlugin m_PluginInstance;

        public ClusteringTool([NotNull] BaseClusteringPlugin pluginInstance)
        {
            m_PluginInstance = pluginInstance;
        }

        [NotNull]
        public IEnumerable<BaseCluster> ClusterElements([NotNull] IEnumerable<Buildable> buildables)
        {
            var output = new List<BaseCluster>();
            var allStructures = buildables.Where(k => k.Asset is ItemStructureAsset).ToList();
            var allBuildables = buildables.Except(allStructures).ToList();

            while (allStructures.Count > 0)
            {
                var elementsOfCluster = new List<Buildable>();

                // Pick a random structure (floor, pillar, wall, etc.)
                var targetStructure = allStructures[Random.Range(0, allStructures.Count)];

                // Add said structure to the current cluster's elements, and remove it from the global sets.
                elementsOfCluster.Add(targetStructure);
                allBuildables.Remove(targetStructure);
                allStructures.Remove(targetStructure);

                // Step 1:
                // Sort all structures by distance to this target structure.
                var sortedFoundations = allStructures.OrderBy(k => (targetStructure.Position - k.Position).sqrMagnitude);

                // Loop through all the sorted structures, adding them to the cluster's elements as we go.

                // ReSharper disable once LoopCanBePartlyConvertedToQuery
                // This disable is since we need to loop through the entire list of sortedFloors as elementsOfCluster changes
                // depending on if its true or false, causing future checks to become true when they were originally false
                foreach (var next in sortedFoundations)
                {
                    // See if 37f is accurate, if not put it in config so it can be changed instead of hardcoded.
                    // Use Mathf.Pow if using config option.
                    if (!elementsOfCluster.Exists(k => (next.Position - k.Position).sqrMagnitude <= 37f))
                        continue;

                    elementsOfCluster.Add(next);
                    allStructures.Remove(next);
                    allBuildables.Remove(next);
                }

                // Step 2:
                // Sort all buildables by distance to the original target structure.
                var sortedBuildables = allBuildables.OrderBy(k => (targetStructure.Position - k.Position).sqrMagnitude);

                // Loop through all the sorted buildables, adding them to the cluster's elemtns as we go.
                // Note that the cluster's distance check doesn't change anymore, as it should only check directly
                // against STRUCTURE types, not all types.
                foreach (var element in sortedBuildables.Where(l => elementsOfCluster.Exists(k => (l.Position - k.Position).sqrMagnitude <= Mathf.Pow(m_PluginInstance.Configuration.Instance.MaxBaseDistanceCheck, 2))))
                {
                    elementsOfCluster.Add(element);
                    allBuildables.Remove(element);
                }

                output.Add(new BaseCluster(m_PluginInstance, false, elementsOfCluster));
            }

            // All other buildables are most likely random barricades that can be groupped up for garbage collection later.
            output.Add(new BaseCluster(m_PluginInstance, true, allBuildables));
            allBuildables.Clear();

            return output;
        }

        [CanBeNull]
        public BaseCluster FindBestCluster([NotNull] IEnumerable<BaseCluster> source, [NotNull] Buildable target)
        {
            return FindBestCluster(source, target.Position);
        }


        [CanBeNull]
        public BaseCluster FindBestCluster([NotNull] IEnumerable<BaseCluster> source, Vector3 target)
        {
            return FindBestClusters(source, target).FirstOrDefault();
        }

        [NotNull]
        public IEnumerable<BaseCluster> FindBestClusters([NotNull] IEnumerable<BaseCluster> source, [NotNull] Buildable target)
        {
            return FindBestClusters(source, target.Position);
        }

        [NotNull]
        public IEnumerable<BaseCluster> FindBestClusters([NotNull] IEnumerable<BaseCluster> source, Vector3 target)
        {
            var allClusters = source.ToList();

            // Remove global cluster. If there's no close enough clusters, this cluster is re-added.
            var globalCluster = allClusters.FirstOrDefault(k => k.IsGlobalCluster);
            if (globalCluster != null)
                allClusters.Remove(globalCluster);

            // Get all clusters that we are close enough to
            var validClusters = allClusters.Where(k => k.IsWithinRange(target)).ToList();

            if (!validClusters.Any())
                validClusters.Add(globalCluster);

            return validClusters.OrderBy(k => (k.AverageCenterPosition - target).sqrMagnitude);
        }
    }
}