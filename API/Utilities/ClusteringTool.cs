using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Buildables;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Pustalorc.Plugins.BaseClustering.API.Utilities
{
    public sealed class ClusteringTool
    {
        private readonly BaseClusteringPlugin m_PluginInstance;

        public ClusteringTool([NotNull] BaseClusteringPlugin pluginInstance)
        {
            m_PluginInstance = pluginInstance;
        }

        [NotNull]
        public IEnumerable<BaseCluster> ClusterElements([NotNull] IEnumerable<Buildable> buildables,
            bool needLogging = false)
        {
            var timer = Stopwatch.StartNew();
            var output = new List<BaseCluster>();
            var allBuildables = buildables.ToList();
            var buildCount = allBuildables.Count;
            var allStructures = allBuildables.OfType<StructureBuildable>().ToList();
            var allBarricades = allBuildables.OfType<BarricadeBuildable>().ToList();
            var maxStructureDistance =
                Mathf.Pow(m_PluginInstance.Configuration.Instance.MaxDistanceBetweenStructures, 2);
            var maxBarricadeDistance =
                Mathf.Pow(m_PluginInstance.Configuration.Instance.MaxDistanceToConsiderPartOfBase, 2);
            var rate = Math.Floor(buildCount * 0.085);
            var currentMultiplier = 0;

            while (allStructures.Count > 0)
            {
                var structuresOfCluster = new List<StructureBuildable>();
                var barricadesOfCluster = new List<BarricadeBuildable>();

                // Pick a random structure (floor, pillar, wall, etc.)
                var targetStructure = allStructures[Random.Range(0, allStructures.Count)];
                allStructures.Remove(targetStructure);
                structuresOfCluster.Add(targetStructure);

                var sortedStructures = allStructures.OrderBy(k => (targetStructure.Position - k.Position).sqrMagnitude);

                foreach (var next in sortedStructures.Where(next => structuresOfCluster.Exists(k =>
                    (next.Position - k.Position).sqrMagnitude <= maxStructureDistance)))
                {
                    structuresOfCluster.Add(next);
                    allStructures.Remove(next);
                }

                var center = allStructures.AverageCenter(l => l.Position);
                var sortedBarricades = allBarricades.OrderBy(k => (center - k.Position).sqrMagnitude);

                foreach (var next in sortedBarricades.Where(next => structuresOfCluster.Exists(k =>
                    (next.Position - k.Position).sqrMagnitude <= maxBarricadeDistance)))
                {
                    barricadesOfCluster.Add(next);
                    allBarricades.Remove(next);
                }

                var clusterBuildables = structuresOfCluster.Cast<Buildable>().Concat(barricadesOfCluster).ToList();
                output.Add(new BaseCluster(m_PluginInstance, false, clusterBuildables));

                var currentBuildCount = output.Sum(k => k.Buildables.Count);

                if (!needLogging || !(currentBuildCount / rate > currentMultiplier)) continue;

                currentMultiplier++;
                Logging.Write("BaseClustering",
                    $"Generating new clusters... {Math.Ceiling(currentBuildCount / (double) buildCount * 100)}% [{currentBuildCount}/{buildCount}] {timer.ElapsedMilliseconds}ms",
                    ConsoleColor.Cyan);
            }

            // All other buildables are most likely random barricades that can be grouped up for garbage collection later.
            // If there's no buildables left, there's no need to create this new cluster.
            if (allBarricades.Count > 0)
                output.Add(new BaseCluster(m_PluginInstance, true, allBarricades.Cast<Buildable>().ToList()));

            var finalBuildCount = output.Sum(k => k.Buildables.Count);
            if (needLogging)
                Logging.Write("BaseClustering",
                    $"Generating new clusters... {Math.Ceiling(finalBuildCount / (double) buildCount * 100)}% [{finalBuildCount}/{buildCount}] {timer.ElapsedMilliseconds}ms",
                    ConsoleColor.Cyan);

            return output;
        }

        [CanBeNull]
        public BaseCluster FindBestCluster([NotNull] Buildable target)
        {
            return FindBestClusters(target).FirstOrDefault();
        }


        [CanBeNull]
        public BaseCluster FindBestCluster(Vector3 target)
        {
            return FindBestClusters(target).FirstOrDefault();
        }

        [NotNull]
        public IEnumerable<BaseCluster> FindBestClusters([NotNull] Buildable target)
        {
            var allClusters = m_PluginInstance.Clusters;
            var globalCluster = allClusters.FirstOrDefault(k => k.IsGlobalCluster);

            // Get all clusters that we are close enough to
            var validClusters = allClusters.Where(k => k.IsWithinRange(target)).ToList();

            // If we have more than 1 cluster, and globalCluster is not null, remove globalCluster from the result
            if (validClusters.Count > 1 && globalCluster != null)
                validClusters.Remove(globalCluster);

            return validClusters.OrderBy(k => (k.AverageCenterPosition - target.Position).sqrMagnitude);
        }

        [NotNull]
        public IEnumerable<BaseCluster> FindBestClusters(Vector3 target)
        {
            var allClusters = m_PluginInstance.Clusters;
            var globalCluster = allClusters.FirstOrDefault(k => k.IsGlobalCluster);

            // Get all clusters that we are close enough to
            var validClusters = allClusters.Where(k => k.IsWithinRange(target)).ToList();

            // If we have more than 1 cluster, and globalCluster is not null, remove globalCluster from the result
            if (validClusters.Count > 1 && globalCluster != null)
                validClusters.Remove(globalCluster);

            return validClusters.OrderBy(k => (k.AverageCenterPosition - target).sqrMagnitude);
        }
    }
}