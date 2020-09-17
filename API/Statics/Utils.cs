using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Classes;
using Pustalorc.Plugins.BaseClustering.Config;
using SDG.Unturned;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.API.Statics
{
    public static class Utils
    {
        [NotNull]
        public static List<BaseCluster> BruteforceClustering([NotNull] List<Buildable> allBuildables,
            BruteforceOptions options, ref ulong instanceCount)
        {
            var output = new List<BaseCluster>();

            while (allBuildables.Count > 0)
            {
                var radius = options.InitialRadius;

                // Get center-most buildable
                var centerIndex = allBuildables.GetCenterIndex();
                var center = allBuildables[centerIndex].Position;

                // Select all buildables that are within the radius of the cluster, but are not outliers
                var localCluster = allBuildables.GetCluster(center, radius);

                // Move cluster centre to local densest point
                centerIndex = localCluster.GetLocalCenterIndex();
                center = allBuildables[centerIndex].Position;

                // Auto expand cluster and select new buildables
                var currentRechecks = 0;
                bool changed;
                do
                {
                    changed = false;
                    currentRechecks++;

                    // Select all buildables that are within the radius of the new center, but are not outliers
                    localCluster = allBuildables.GetCluster(center, radius);

                    // Update radii to maximum distance
                    var radiiDist = localCluster.GetDistances(center);

                    if (!(radiiDist.Values.Max() > 0)) continue;

                    var newRadius = radiiDist.Values.Max();

                    if (newRadius != radius)
                        changed = true;

                    if (newRadius > options.MaxRadius)
                        newRadius = options.MaxRadius;

                    radius = newRadius;
                } while (changed && options.MaxRadiusRechecks > currentRechecks &&
                         radius < options.MaxRadius);

                // Assign data to final clusters
                var builds = new List<Buildable>();
                for (var i = 0; i < localCluster.Count; i++)
                {
                    builds.Add(allBuildables[localCluster.ElementAt(i).Key - i]);
                    allBuildables.RemoveAt(localCluster.ElementAt(i).Key - i);
                }

                output.Add(new BaseCluster(builds, center, radius, false, instanceCount++));
            }

            return output;
        }

        [NotNull]
        public static List<BaseCluster> RustClustering([NotNull] List<Buildable> allBuildables, RustOptions options,
            bool remainingIntoOmegaCluster, ref ulong instanceCount)
        {
            var output = new List<BaseCluster>();

            var allFloors = allBuildables
                .Where(k => k.Asset is ItemStructureAsset && options.FloorIds.Contains(k.AssetId)).ToList();

            while (allFloors.Count > 0)
            {
                var elementsOfCluster = new List<Buildable>();

                var floor = allFloors[0];
                elementsOfCluster.Add(floor);
                allBuildables.Remove(floor);
                allFloors.Remove(floor);

                var sortedFloors = allFloors.OrderBy(k => Vector3.Distance(floor.Position, k.Position));
                var sortedBuildables = allBuildables.OrderBy(k => Vector3.Distance(floor.Position, k.Position));

                // ReSharper disable once LoopCanBePartlyConvertedToQuery
                // This is since we need to loop through the entire list of sortedFloors as elementsOfCluster changes
                // depending on if its true or false, causing future checks to become true when they were originally false
                foreach (var next in sortedFloors)
                {
                    if (!elementsOfCluster.Exists(k => Vector3.Distance(next.Position, k.Position) <= 6.1f))
                        continue;

                    elementsOfCluster.Add(next);
                    allFloors.Remove(next);
                    allBuildables.Remove(next);
                }

                // ReSharper disable once LoopCanBePartlyConvertedToQuery
                // This is since we need to loop through the entire list of sortedFloors as elementsOfCluster changes
                // depending on if its true or false, causing future checks to become true when they were originally false
                foreach (var element in sortedBuildables)
                {
                    if (!elementsOfCluster.Exists(k =>
                        Vector3.Distance(element.Position, k.Position) <= options.ExtraRadius)) continue;

                    elementsOfCluster.Add(element);
                    allBuildables.Remove(element);
                }

                var centerIndex = elementsOfCluster.GetCenterIndex();
                var centerBuild = elementsOfCluster[centerIndex];
                output.Add(new BaseCluster(elementsOfCluster, centerBuild.Position,
                    elementsOfCluster.GetDistances(k => k.Position, centerBuild.Position).Max() + options.ExtraRadius,
                    false, instanceCount++));
            }

            if (!remainingIntoOmegaCluster) return output;

            var center = allBuildables.AverageCenter(k => k.Position);
            output.Add(new BaseCluster(allBuildables, center,
                allBuildables.GetDistances(k => k.Position, center).Max() + options.ExtraRadius, true,
                instanceCount++));
            allBuildables.Clear();

            return output;
        }

        [NotNull]
        public static List<BaseCluster> HybridClustering([NotNull] List<Buildable> allBuildables,
            BruteforceOptions bruteforceOptions, RustOptions rustOptions, ref ulong instanceCount)
        {
            var output = RustClustering(allBuildables, rustOptions, false, ref instanceCount);

            output.AddRange(BruteforceClustering(allBuildables, bruteforceOptions, ref instanceCount));

            return output;
        }
    }
}