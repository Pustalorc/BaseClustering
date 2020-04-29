using Pustalorc.Plugins.BaseClustering.API.Statics;
using Rocket.Core.Plugins;
using SDG.Unturned;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Pustalorc.Plugins.BaseClustering.API.Classes.Objects;
using Rocket.Core.Utils;
using Steamworks;
using UnityEngine;
using Math = System.Math;
using System.Collections.Specialized;
using System;
using Pustalorc.Plugins.BaseClustering.Config;

namespace Pustalorc.Plugins.BaseClustering
{
    public class BaseClusteringPlugin : RocketPlugin<BaseClusteringPluginConfiguration>
    {
        public static BaseClusteringPlugin Instance { get; private set; }

        protected override void Load()
        {
            Instance = this;

            if (Level.isLoaded)
                OnLevelLoaded(0);
            else
                Level.onLevelLoaded += OnLevelLoaded;

            Logging.PluginLoaded(this);
        }

        protected override void Unload()
        {
            StructureManager.onDamageStructureRequested -= StructureDamaged;
            StructureManager.onDeployStructureRequested -= StructureDeployed;
            StructureManager.onSalvageStructureRequested -= StructureSalvaged;
            StructureManager.onStructureSpawned -= StructureSpawned;
            StructureManager.onTransformRequested -= StructureTransformed;
            BarricadeManager.onDamageBarricadeRequested -= BarricadeDamaged;
            BarricadeManager.onDeployBarricadeRequested -= BarricadeDeployed;
            BarricadeManager.onSalvageBarricadeRequested -= BarricadeSalvaged;
            BarricadeManager.onBarricadeSpawned -= BarricadeSpawned;
            BarricadeManager.onTransformRequested -= BarricadeTransformed;

            Instance = null;

            Logging.PluginUnloaded(this);
        }

        public List<BaseCluster> Clusters { get; set; }

        /// <summary>
        ///     Retrieves all clusters within the specified radius.
        /// </summary>
        /// <param name="center">The position to search from.</param>
        /// <param name="sqrRadius">The maximum distance to detect a cluster in.</param>
        /// <returns></returns>
        public IEnumerable<BaseCluster> GetClustersInRadius(Vector3 center, float sqrRadius)
        {
            return Clusters?.Where(k => Vector3.Distance(k.Center, center) < sqrRadius) ??
                   new List<BaseCluster>();
        }

        /// <summary>
        ///     Changes all owners and groups of a cluster to match a specific one.
        /// </summary>
        /// <param name="cluster">The specific cluster to change the groups in.</param>
        /// <param name="newOwner">The new owner of all the buildables.</param>
        /// <param name="newGroup">The new group of all the buildables.</param>
        public void ChangeOwnerAndGroup(BaseCluster cluster, ulong newOwner, ulong newGroup)
        {
            if (ThreadUtil2.IsGameThread)
                _changeOwnerAndGroup(cluster, newOwner, newGroup);
            else
                TaskDispatcher.QueueOnMainThread(() => _changeOwnerAndGroup(cluster, newOwner, newGroup));
        }

        /// <summary>
        ///     Damages all buildables from within a cluster.
        /// </summary>
        /// <param name="cluster">The specific cluster to damage the buildables in.</param>
        /// <param name="damage">The amount of damage to deal to the buildables.</param>
        /// <param name="instigator">The originating CSteamID of the damage.</param>
        /// <param name="damageOrigin">The type of the originated damage.</param>
        public void Damage(BaseCluster cluster, ushort damage, CSteamID instigator, EDamageOrigin damageOrigin)
        {
            if (ThreadUtil2.IsGameThread)
                _damage(cluster, damage, instigator, damageOrigin);
            else
                TaskDispatcher.QueueOnMainThread(() => _damage(cluster, damage, instigator, damageOrigin));
        }

        /// <summary>
        ///     Destroys a specific cluster from the map, including all its buildables.
        /// </summary>
        /// <param name="cluster">The specific cluster to destroy.</param>
        public void DestroyCluster(BaseCluster cluster)
        {
            if (ThreadUtil2.IsGameThread)
                _destroyCluster(cluster);
            else
                TaskDispatcher.QueueOnMainThread(() => _destroyCluster(cluster));
        }

        /// <summary>
        ///     Repairs all buildables that are inside a specific cluster.
        /// </summary>
        /// <param name="cluster">The specific cluster of which to repair buildables from.</param>
        /// <param name="amount">The amount of health to repair it by.</param>
        /// <param name="times">The multiplier for the amount of health to be repaired.</param>
        public void Repair(BaseCluster cluster, float amount, float times)
        {
            if (ThreadUtil2.IsGameThread)
                _repair(cluster, amount, times);
            else
                TaskDispatcher.QueueOnMainThread(() => _repair(cluster, amount, times));
        }

        /// <summary>
        ///     Deletes all clusters, including all buildables, from the map.
        /// </summary>
        public void RemoveAllClusters()
        {
            if (ThreadUtil2.IsGameThread)
                _removeAllClusters();
            else
                TaskDispatcher.QueueOnMainThread(_removeAllClusters);
        }

        private void OnLevelLoaded(int level)
        {
            if (level <= Level.BUILD_INDEX_SETUP) return;

            GenerateAndLoadAllClusters();

            BarricadeManager.onTransformRequested += BarricadeTransformed;
            BarricadeManager.onBarricadeSpawned += BarricadeSpawned;
            BarricadeManager.onSalvageBarricadeRequested += BarricadeSalvaged;
            BarricadeManager.onDeployBarricadeRequested += BarricadeDeployed;
            BarricadeManager.onDamageBarricadeRequested += BarricadeDamaged;
            StructureManager.onTransformRequested += StructureTransformed;
            StructureManager.onStructureSpawned += StructureSpawned;
            StructureManager.onSalvageStructureRequested += StructureSalvaged;
            StructureManager.onDeployStructureRequested += StructureDeployed;
            StructureManager.onDamageStructureRequested += StructureDamaged;
        }

        private void StructureDamaged(CSteamID instigatorSteamID, Transform structureTransform, ref ushort pendingTotalDamage, ref bool shouldAllow, EDamageOrigin damageOrigin)
        {
        }

        private void StructureDeployed(Structure structure, ItemStructureAsset asset, ref Vector3 point, ref float angle_x, ref float angle_y, ref float angle_z, ref ulong owner, ref ulong group, ref bool shouldAllow)
        {
        }

        private void StructureSalvaged(CSteamID steamID, byte x, byte y, ushort index, ref bool shouldAllow)
        {
        }

        private void StructureSpawned(StructureRegion region, StructureDrop drop)
        {
        }

        private void StructureTransformed(CSteamID instigator, byte x, byte y, uint instanceID, ref Vector3 point, ref byte angle_x, ref byte angle_y, ref byte angle_z, ref bool shouldAllow)
        {
        }

        private void BarricadeDamaged(CSteamID instigatorSteamID, Transform barricadeTransform, ref ushort pendingTotalDamage, ref bool shouldAllow, EDamageOrigin damageOrigin)
        {
        }

        private void BarricadeDeployed(Barricade barricade, ItemBarricadeAsset asset, Transform hit, ref Vector3 point, ref float angle_x, ref float angle_y, ref float angle_z, ref ulong owner, ref ulong group, ref bool shouldAllow)
        {
        }

        private void BarricadeSalvaged(CSteamID steamID, byte x, byte y, ushort plant, ushort index, ref bool shouldAllow)
        {
            if (!BarricadeManager.tryGetRegion(x, y, plant, out var region)) return;

            var clusters = Clusters.Where(k => k.Buildables.Any(k => k.InstanceId == region.barricades[index].instanceID));

            foreach (var cluster in clusters)
            {
                if (cluster.Buildables.Count == 1)
                {
                    DestroyCluster(cluster);
                    continue;
                }

                var buildable = cluster.Buildables.FirstOrDefault(k => k.InstanceId == region.barricades[index].instanceID);
                Game.RemoveBarricadeStructure(buildable.Position);
                cluster.Buildables.Remove(buildable);
            }
        }

        private void BarricadeSpawned(BarricadeRegion region, BarricadeDrop drop)
        {
        }

        private void BarricadeTransformed(CSteamID instigator, byte x, byte y, ushort plant, uint instanceID, ref Vector3 point, ref byte angle_x, ref byte angle_y, ref byte angle_z, ref bool shouldAllow)
        {
        }

        private void _changeOwnerAndGroup(BaseCluster cluster, ulong newOwner, ulong newGroup)
        {
            foreach (var buildable in cluster.Buildables)
            {
                Game.ChangeOwnerAndGroup(buildable.Position, newOwner, newGroup);
                buildable.Owner = newOwner;
                buildable.Group = newGroup;
            }
        }

        private void _damage(BaseCluster cluster, ushort damage, CSteamID instigator, EDamageOrigin damageOrigin)
        {
            foreach (var buildable in cluster.Buildables)
                Game.DamageBarricadeStructure(buildable.Position, damage);
        }

        private void _destroyCluster(BaseCluster cluster)
        {
            foreach (var buildable in cluster.Buildables.ToList())
            {
                Game.RemoveBarricadeStructure(buildable.Position);
                cluster.Buildables.Remove(buildable);
            }

            Clusters.Remove(cluster);
        }

        private void _repair(BaseCluster cluster, float amount, float times)
        {
            foreach (var buildable in cluster.Buildables)
                Game.RepairBarricadeStructure(buildable.Position, amount, times);
        }

        private void _removeAllClusters()
        {
            foreach (var cluster in Clusters.ToList())
            {
                foreach (var buildable in cluster.Buildables.ToList())
                {
                    Game.RemoveBarricadeStructure(buildable.Position);
                    cluster.Buildables.Remove(buildable);
                }

                Clusters.Remove(cluster);
            }
        }

        private void GenerateAndLoadAllClusters()
        {
            Clusters = new List<BaseCluster>();

            var allBuildables = Game.GetBuilds(CSteamID.Nil).ToList();

            while (allBuildables.Count > 0)
            {
                var radius = Configuration.Instance.InitialRadius;
                var center = Vector3.zero;

                // Find Center-most Point
                var globalMean = allBuildables.AverageCenter(k => k.Position);
                var globalScalar = allBuildables.GetScalar(k => k.Position);
                var globalDensity = allBuildables.GetDensity(k => k.Position, globalMean, globalScalar);
                var centerIndex = globalDensity.IndexOf(globalDensity.MinVector3());

                // Find Points Belonging to Cluster
                var include =
                    allBuildables.SubtractMaintainOriginalIndices(k => k.Position, allBuildables[centerIndex].Position);
                var radSq = Math.Pow(radius, 2);
                include = include.DivideDictionaryVector3(radSq);
                include = include.GetMatchingWithOriginal(allBuildables.Select(k => k.Position).ToList(), v => v.x < 1 && v.y < 1 && v.z < 1);

                // Remove outliers
                var dist = include.GetDistances(allBuildables[centerIndex].Position);
                var average = dist.Values.Average();
                for (var i = 0; i < dist.Count; i++)
                {
                    var std = ExtendedMath.StandardDeviation(dist.Values);

                    if (double.IsNaN(std))
                        continue;

                    if (dist.Values.ToList()[i] - average <= 3 * std)
                        continue;

                    include.Remove(i);
                    dist.Remove(dist.Keys.ToList()[i]);
                    i--;
                }

                // Move cluster centre to local densest point
                var locMean = include.Values.AverageCenter();
                var locScalar = include.GetLocalScalar();
                var locDens = include.GetLocalDensity(locMean, locScalar);
                centerIndex = locDens.MinVector3();
                center = allBuildables[centerIndex].Position;
                var currentRechecks = 0;
                var changed = false;

                do
                {
                    changed = false;
                    currentRechecks++;
                    // Assign data to new centre
                    include = allBuildables.SubtractMaintainOriginalIndices(k => k.Position, center);
                    radSq = Math.Pow(radius, 2);
                    include = include.DivideDictionaryVector3(radSq);
                    include = include.GetMatchingWithOriginal(allBuildables.Select(k => k.Position).ToList(), v => v.x < 1 && v.y < 1 && v.z < 1);

                    // Remove outliers
                    dist = include.GetDistances(center);
                    average = dist.Values.Average();
                    for (var i = 0; i < dist.Count; i++)
                    {
                        var std = ExtendedMath.StandardDeviation(dist.Values);

                        if (double.IsNaN(std))
                            continue;

                        if (dist.Values.ToList()[i] - average <= 3 * std)
                            continue;

                        include.Remove(i);
                        dist.Remove(dist.Keys.ToList()[i]);
                        i--;
                    }

                    // Update radii to maximum distance
                    var radiiDist = include.GetDistances(center);
                    if (radiiDist.Values.Max() > 0)
                    {
                        var newRadius = radiiDist.Values.Max();

                        if (newRadius != radius)
                            changed = true;

                        if (newRadius > Configuration.Instance.MaxRadius)
                            newRadius = Configuration.Instance.MaxRadius;

                        radius = newRadius;
                    }
                }
                while (changed && Configuration.Instance.MaxRadiusRechecks > currentRechecks && radius < Configuration.Instance.MaxRadius);

                // Assign data to final clusters
                var builds = new List<Buildable>();
                for (var i = 0; i < include.Count; i++)
                {
                    builds.Add(allBuildables[include.ElementAt(i).Key - i]);
                    allBuildables.RemoveAt(include.ElementAt(i).Key - i);
                }

                Clusters.Add(new BaseCluster(builds, center, radius));
            }

            Logging.Write(this, $"Clusters Loaded: {Clusters.Count}");
        }
    }
}