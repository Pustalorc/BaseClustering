using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Buildables;
using Pustalorc.Plugins.BaseClustering.Config;
using SDG.Unturned;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.API.Utils
{
    public sealed class ClusteringTool
    {
        private List<ushort> m_FloorIds;
        private float m_ExtraRadius;

        public ClusteringTool([NotNull] BaseClusteringPluginConfiguration config)
        {
            m_FloorIds = config.FloorItemIds;
            m_ExtraRadius = config.ExtraRadius;
        }

        [NotNull]
        public IEnumerable<BaseCluster> ClusterElements([NotNull] IEnumerable<Buildable> allBuildables,
            ref ulong clusterInstanceCount)
        {
            var output = new List<BaseCluster>();
            var builds = allBuildables.ToList();
            var allFloors = builds.Where(k => k.Asset is ItemStructureAsset && m_FloorIds.Contains(k.AssetId)).ToList();

            while (allFloors.Count > 0)
            {
                var elementsOfCluster = new List<Buildable>();

                var floor = allFloors[0];
                elementsOfCluster.Add(floor);
                builds.Remove(floor);
                allFloors.Remove(floor);

                var sortedFloors = allFloors.OrderBy(k => Vector3.Distance(floor.Position, k.Position));
                var sortedBuildables = builds.OrderBy(k => Vector3.Distance(floor.Position, k.Position));

                // ReSharper disable once LoopCanBePartlyConvertedToQuery
                // This is since we need to loop through the entire list of sortedFloors as elementsOfCluster changes
                // depending on if its true or false, causing future checks to become true when they were originally false
                foreach (var next in sortedFloors)
                {
                    if (!elementsOfCluster.Exists(k => Vector3.Distance(next.Position, k.Position) <= 6.1f))
                        continue;

                    elementsOfCluster.Add(next);
                    allFloors.Remove(next);
                    builds.Remove(next);
                }

                // ReSharper disable once LoopCanBePartlyConvertedToQuery
                // This is since we need to loop through the entire list of sortedFloors as elementsOfCluster changes
                // depending on if its true or false, causing future checks to become true when they were originally false
                foreach (var element in sortedBuildables)
                {
                    if (!elementsOfCluster.Exists(k =>
                        Vector3.Distance(element.Position, k.Position) <= m_ExtraRadius)) continue;

                    elementsOfCluster.Add(element);
                    builds.Remove(element);
                }

                var clusterCenter = elementsOfCluster.AverageCenter(k => k.Position);
                output.Add(new BaseCluster(elementsOfCluster,
                    elementsOfCluster.GetDistances(k => k.Position, clusterCenter).Max() + m_ExtraRadius, false,
                    clusterInstanceCount++));
            }

            var center = builds.AverageCenter(k => k.Position);
            output.Add(new BaseCluster(builds, builds.GetDistances(k => k.Position, center).Max() + m_ExtraRadius, true,
                clusterInstanceCount++));
            builds.Clear();

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
        public IEnumerable<BaseCluster> FindBestClusters([NotNull] IEnumerable<BaseCluster> source,
            [NotNull] Buildable target)
        {
            return FindBestClusters(source, target.Position);
        }

        [NotNull]
        public IEnumerable<BaseCluster> FindBestClusters([NotNull] IEnumerable<BaseCluster> source, Vector3 target)
        {
            var allClusters = source.ToList();

            // Get global cluster (rust only)
            var globalCluster = allClusters.FirstOrDefault(k => k.IsGlobalCluster);
            if (globalCluster != null)
                allClusters.Remove(globalCluster);

            // Get all clusters that we are close enough to
            var validClusters = allClusters.Where(k =>
            {
                var distance = Vector3.Distance(k.AverageCenterPosition, target);
                return distance <= k.Radius || distance <= k.Radius + m_ExtraRadius;
            }).ToList();

            if (!validClusters.Any())
                validClusters.Add(globalCluster);

            return validClusters.OrderBy(k => Vector3.Distance(k.AverageCenterPosition, target));
        }

        [CanBeNull]
        public BaseCluster FindBestClusterWithMaxDistance([NotNull] IEnumerable<BaseCluster> source,
            [NotNull] Buildable target)
        {
            return FindBestCluster(source, target.Position);
        }

        [CanBeNull]
        public BaseCluster FindBestClusterWithMaxDistance([NotNull] IEnumerable<BaseCluster> source, Vector3 target)
        {
            return FindBestClustersWithMaxDistance(source, target).FirstOrDefault();
        }

        [NotNull]
        public IEnumerable<BaseCluster> FindBestClustersWithMaxDistance([NotNull] IEnumerable<BaseCluster> source,
            [NotNull] Buildable target)
        {
            return FindBestClustersWithMaxDistance(source, target.Position);
        }

        [NotNull]
        public IEnumerable<BaseCluster> FindBestClustersWithMaxDistance([NotNull] IEnumerable<BaseCluster> source,
            Vector3 target)
        {
            var allClusters = source.ToList();

            // Get global cluster (rust only)
            var globalCluster = allClusters.FirstOrDefault(k => k.IsGlobalCluster);
            if (globalCluster != null)
                allClusters.Remove(globalCluster);

            // Get all clusters that we are close enough to
            var validClusters = allClusters.Where(k =>
            {
                var distance = Vector3.Distance(k.AverageCenterPosition, target);
                return distance <= k.Radius || distance <= m_ExtraRadius;
            }).ToList();

            if (!validClusters.Any())
                validClusters.Add(globalCluster);

            return validClusters.OrderBy(k => Vector3.Distance(k.AverageCenterPosition, target));
        }
    }
}