using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Classes;
using Pustalorc.Plugins.BaseClustering.API.Statics;
using Pustalorc.Plugins.BaseClustering.Config;
using Pustalorc.Plugins.BaseClustering.Patches;
using Rocket.Core.Plugins;
using Rocket.Core.Utils;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

// ReSharper disable MemberCanBeMadeStatic.Local

namespace Pustalorc.Plugins.BaseClustering
{
    public sealed class BaseClusteringPlugin : RocketPlugin<BaseClusteringPluginConfiguration>
    {
        public static BaseClusteringPlugin Instance { get; private set; }
        private Harmony _harmony;

        protected override void Load()
        {
            Instance = this;
            _harmony = new Harmony("xyz.pustalorc.baseClustering");
            _harmony.PatchAll();

            if (Level.isLoaded)
                OnLevelLoaded(0);
            else
                Level.onLevelLoaded += OnLevelLoaded;

            Logging.PluginLoaded(this);
        }

        protected override void Unload()
        {
            BarricadeManager.onSalvageBarricadeRequested -= BarricadeSalvaged;
            BarricadeManager.onDamageBarricadeRequested -= BarricadeDamaged;
            BarricadeManager.onHarvestPlantRequested -= PlantHarvested;
            BarricadeManager.onTransformRequested -= BarricadeTransformed;
            PatchBarricadeSpawnInternal.OnNewBarricadeSpawned -= BarricadeSpawned;

            StructureManager.onTransformRequested -= StructureTransformed;
            StructureManager.onSalvageStructureRequested -= StructureSalvaged;
            StructureManager.onDamageStructureRequested -= StructureDamaged;
            PatchStructureSpawnInternal.OnNewStructureSpawned -= StructureSpawned;

            _harmony.UnpatchAll();
            _harmony = null;

            Instance = null;

            Logging.PluginUnloaded(this);
        }

        public List<BaseCluster> Clusters { get; set; }

        public List<Buildable> Buildables => Clusters?.SelectMany(k => k.Buildables).ToList() ?? new List<Buildable>();

        /// <summary>
        ///     Retrieves all clusters within the specified radius.
        /// </summary>
        /// <param name="center">The position to search from.</param>
        /// <param name="sqrRadius">The maximum distance to detect a cluster in.</param>
        /// <returns></returns>
        [NotNull]
        public IEnumerable<BaseCluster> GetClustersInRadius(Vector3 center, float sqrRadius)
        {
            return Clusters?.Where(k => Vector3.Distance(k.CenterBuildable, center) < sqrRadius) ??
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
        public void Damage(BaseCluster cluster, ushort damage)
        {
            if (ThreadUtil2.IsGameThread)
                _damage(cluster, damage);
            else
                TaskDispatcher.QueueOnMainThread(() => _damage(cluster, damage));
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

        internal void GenerateAndLoadAllClusters()
        {
            var start = DateTime.Now;
            Clusters = new List<BaseCluster>();

            var allBuildables = Game.GetBuilds(CSteamID.Nil).ToList();
            Logging.Write(this, $"Total buildables: {allBuildables.Count}");

            while (allBuildables.Count > 0)
            {
                var radius = Configuration.Instance.InitialRadius;

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

                    if (newRadius > Configuration.Instance.MaxRadius)
                        newRadius = Configuration.Instance.MaxRadius;

                    radius = newRadius;
                } while (changed && Configuration.Instance.MaxRadiusRechecks > currentRechecks &&
                         radius < Configuration.Instance.MaxRadius);

                // Assign data to final clusters
                var builds = new List<Buildable>();
                for (var i = 0; i < localCluster.Count; i++)
                {
                    builds.Add(allBuildables[localCluster.ElementAt(i).Key - i]);
                    allBuildables.RemoveAt(localCluster.ElementAt(i).Key - i);
                }

                var cluster = new BaseCluster(builds, center, radius);
                Clusters.Add(cluster);
                Logging.Verbose(this, $"New cluster created at: {center}\nRadius: {radius}\nAverage Center: {cluster.AverageCenterPosition}\nMost common group: {cluster.CommonGroup}\nMost common owner: {cluster.CommonOwner}\nAll buildables: {string.Join(", ", cluster.Buildables.Select(k => k.Position))}");
            }

            var end = DateTime.Now;

            Logging.Write(this,
                $"Clusters Loaded: {Clusters.Count}. Took {(int)end.Subtract(start).TotalMilliseconds}ms.");
        }

        private void OnLevelLoaded(int level)
        {
            GenerateAndLoadAllClusters();

            BarricadeManager.onSalvageBarricadeRequested += BarricadeSalvaged;
            BarricadeManager.onDamageBarricadeRequested += BarricadeDamaged;
            BarricadeManager.onHarvestPlantRequested += PlantHarvested;
            BarricadeManager.onTransformRequested += BarricadeTransformed;
            PatchBarricadeSpawnInternal.OnNewBarricadeSpawned += BarricadeSpawned;

            StructureManager.onTransformRequested += StructureTransformed;
            StructureManager.onSalvageStructureRequested += StructureSalvaged;
            StructureManager.onDamageStructureRequested += StructureDamaged;
            PatchStructureSpawnInternal.OnNewStructureSpawned += StructureSpawned;
        }

        private void StructureDamaged(CSteamID instigatorSteamId, Transform structureTransform,
            ref ushort pendingTotalDamage, ref bool shouldAllow, EDamageOrigin damageOrigin)
        {
            if (!shouldAllow || !StructureManager.tryGetInfo(structureTransform, out var x, out var y, out var index, out var region)) return;

            var sData = region.structures[index];

            if (sData.structure.isDead)
            {
                foreach (var cluster in Clusters.Where(k => k.Buildables.Any(l => l.InstanceId == sData.instanceID)).ToList())
                {
                    if (cluster.Buildables.Count == 1)
                    {
                        DestroyCluster(cluster);
                        continue;
                    }

                    var buildable =
                        cluster.Buildables.FirstOrDefault(k => k.InstanceId == sData.instanceID);
                    if (buildable == null)
                    {
                        Logging.Verbose(this,
                            "Missed a buildable at some point. Unable to remove from cluster when salvaged.");
                        continue;
                    }

                    Game.RemoveBarricadeStructure(buildable.Position);
                    cluster.Buildables.Remove(buildable);
                }
                return;
            }

            if (pendingTotalDamage < 1 || pendingTotalDamage < sData.structure.health) return;

            var clusters = Clusters.Where(k =>
                k.Buildables.Any(l => l.InstanceId == sData.instanceID));

            foreach (var cluster in clusters.ToList())
            {
                if (cluster.Buildables.Count == 1)
                {
                    DestroyCluster(cluster);
                    continue;
                }

                var buildable =
                    cluster.Buildables.FirstOrDefault(k => k.InstanceId == sData.instanceID);
                if (buildable == null)
                {
                    Logging.Verbose(this,
                        "Missed a buildable at some point. Unable to remove from cluster when salvaged.");
                    continue;
                }

                Game.RemoveBarricadeStructure(buildable.Position);
                cluster.Buildables.Remove(buildable);
            }
        }

        private void StructureSpawned(StructureData data, StructureDrop drop)
        {
            var buildable = new Buildable(data.angle_x, data.angle_y, data.angle_z, data.structure.id, data.structure.health, data.instanceID, data.owner, data.group, data.point, data.structure.asset, drop.model, null, null);

            var bestCluster = Clusters.FindBestCluster(buildable, Configuration.Instance.MaxRadius);

            if (bestCluster == null)
            {
                Clusters.Add(new BaseCluster(new List<Buildable> { buildable }, buildable.Position, Configuration.Instance.InitialRadius));
                return;
            }

            bestCluster.Buildables.Add(buildable);
        }

        private void StructureSalvaged(CSteamID steamId, byte x, byte y, ushort index, ref bool shouldAllow)
        {
            if (!shouldAllow || !StructureManager.tryGetRegion(x, y, out var region)) return;

            var clusters = Clusters.Where(k =>
                k.Buildables.Any(l => l.InstanceId == region.structures[index].instanceID));

            foreach (var cluster in clusters.ToList())
            {
                if (cluster.Buildables.Count == 1)
                {
                    DestroyCluster(cluster);
                    continue;
                }

                var buildable =
                    cluster.Buildables.FirstOrDefault(k => k.InstanceId == region.structures[index].instanceID);
                if (buildable == null)
                {
                    Logging.Verbose(this,
                        "Missed a buildable at some point. Unable to remove from cluster when salvaged.");
                    continue;
                }

                Game.RemoveBarricadeStructure(buildable.Position);
                cluster.Buildables.Remove(buildable);
            }
        }

        private void StructureTransformed(CSteamID instigator, byte x, byte y, uint instanceId, ref Vector3 point,
            ref byte angleX, ref byte angleY, ref byte angleZ, ref bool shouldAllow)
        {
            if (!shouldAllow) return;

            var cluster = Clusters.FirstOrDefault(k => k.Buildables.Any(k => k.InstanceId == instanceId));
            if (cluster == null)
            {
                Logging.Verbose(this, $"Missed a barricade being added with instance ID {instanceId}");
                return;
            }

            var buildable = cluster.Buildables.FirstOrDefault(k => k.InstanceId == instanceId);
            cluster.Buildables.Remove(buildable);

            if (cluster.Buildables.Count == 0)
                DestroyCluster(cluster);

            buildable.AngleX = angleX;
            buildable.AngleY = angleY;
            buildable.AngleZ = angleZ;
            buildable.Position = point;

            var bestCluster = Clusters.FindBestCluster(buildable, Configuration.Instance.MaxRadius);

            if (bestCluster == null)
            {
                Clusters.Add(new BaseCluster(new List<Buildable> { buildable }, buildable.Position, Configuration.Instance.InitialRadius));
                return;
            }

            bestCluster.Buildables.Add(buildable);
        }

        private void BarricadeDamaged(CSteamID instigatorSteamId, Transform barricadeTransform,
            ref ushort pendingTotalDamage, ref bool shouldAllow, EDamageOrigin damageOrigin)
        {
            if (!shouldAllow || !BarricadeManager.tryGetInfo(barricadeTransform, out var x, out var y, out var plant, out var index, out var region)) return;

            var bData = region.barricades[index];

            if (bData.barricade.isDead)
            {
                foreach (var cluster in Clusters.Where(k => k.Buildables.Any(l => l.InstanceId == bData.instanceID)).ToList())
                {
                    if (cluster.Buildables.Count == 1)
                    {
                        DestroyCluster(cluster);
                        continue;
                    }

                    var buildable =
                        cluster.Buildables.FirstOrDefault(k => k.InstanceId == bData.instanceID);
                    if (buildable == null)
                    {
                        Logging.Verbose(this,
                            "Missed a buildable at some point. Unable to remove from cluster when salvaged.");
                        continue;
                    }

                    Game.RemoveBarricadeStructure(buildable.Position);
                    cluster.Buildables.Remove(buildable);
                }
                return;
            }

            if (pendingTotalDamage < 1 || pendingTotalDamage < bData.barricade.health) return;

            var clusters = Clusters.Where(k =>
                k.Buildables.Any(l => l.InstanceId == bData.instanceID));

            foreach (var cluster in clusters.ToList())
            {
                if (cluster.Buildables.Count == 1)
                {
                    DestroyCluster(cluster);
                    continue;
                }

                var buildable =
                    cluster.Buildables.FirstOrDefault(k => k.InstanceId == bData.instanceID);
                if (buildable == null)
                {
                    Logging.Verbose(this,
                        "Missed a buildable at some point. Unable to remove from cluster when salvaged.");
                    continue;
                }

                Game.RemoveBarricadeStructure(buildable.Position);
                cluster.Buildables.Remove(buildable);
            }
        }

        private void BarricadeSpawned(BarricadeData data, BarricadeDrop drop)
        {
            var buildable = new Buildable(data.angle_x, data.angle_y, data.angle_z, data.barricade.id, data.barricade.health, data.instanceID, data.owner, data.group, data.point, drop.asset, drop.model, drop.interactable, data.barricade.state);

            var bestCluster = Clusters.FindBestCluster(buildable, Configuration.Instance.MaxRadius);

            if (bestCluster == null)
            {
                Clusters.Add(new BaseCluster(new List<Buildable> { buildable }, buildable.Position, Configuration.Instance.InitialRadius));
                return;
            }

            bestCluster.Buildables.Add(buildable);
        }

        private void BarricadeSalvaged(CSteamID steamId, byte x, byte y, ushort plant, ushort index,
            ref bool shouldAllow)
        {
            if (!shouldAllow || !BarricadeManager.tryGetRegion(x, y, plant, out var region)) return;

            var clusters = Clusters.Where(k =>
                k.Buildables.Any(l => l.InstanceId == region.barricades[index].instanceID));

            foreach (var cluster in clusters.ToList())
            {
                if (cluster.Buildables.Count == 1)
                {
                    DestroyCluster(cluster);
                    continue;
                }

                var buildable =
                    cluster.Buildables.FirstOrDefault(k => k.InstanceId == region.barricades[index].instanceID);
                if (buildable == null)
                {
                    Logging.Verbose(this,
                        "Missed a buildable at some point. Unable to remove from cluster when salvaged.");
                    continue;
                }

                Game.RemoveBarricadeStructure(buildable.Position);
                cluster.Buildables.Remove(buildable);
            }
        }

        private void PlantHarvested(CSteamID steamId, byte x, byte y, ushort plant, ushort index, ref bool shouldAllow)
        {
            if (!shouldAllow || !BarricadeManager.tryGetRegion(x, y, plant, out var region)) return;

            var clusters = Clusters.Where(k =>
                k.Buildables.Any(l => l.InstanceId == region.barricades[index].instanceID));

            foreach (var cluster in clusters.ToList())
            {
                if (cluster.Buildables.Count == 1)
                {
                    DestroyCluster(cluster);
                    continue;
                }

                var buildable =
                    cluster.Buildables.FirstOrDefault(k => k.InstanceId == region.barricades[index].instanceID);
                if (buildable == null)
                {
                    Logging.Verbose(this,
                        "Missed a buildable at some point. Unable to remove from cluster when salvaged.");
                    continue;
                }

                Game.RemoveBarricadeStructure(buildable.Position);
                cluster.Buildables.Remove(buildable);
            }
        }

        private void BarricadeTransformed(CSteamID instigator, byte x, byte y, ushort plant, uint instanceId,
            ref Vector3 point, ref byte angleX, ref byte angleY, ref byte angleZ, ref bool shouldAllow)
        {
            if (!shouldAllow) return;

            var cluster = Clusters.FirstOrDefault(k => k.Buildables.Any(k => k.InstanceId == instanceId));
            if (cluster == null)
            {
                Logging.Verbose(this, $"Missed a barricade being added with instance ID {instanceId}");
                return;
            }

            var buildable = cluster.Buildables.FirstOrDefault(k => k.InstanceId == instanceId);            
            cluster.Buildables.Remove(buildable);

            if (cluster.Buildables.Count == 0)
                DestroyCluster(cluster);

            buildable.AngleX = angleX;
            buildable.AngleY = angleY;
            buildable.AngleZ = angleZ;
            buildable.Position = point;

            var bestCluster = Clusters.FindBestCluster(buildable, Configuration.Instance.MaxRadius);

            if (bestCluster == null)
            {
                Clusters.Add(new BaseCluster(new List<Buildable> { buildable }, buildable.Position, Configuration.Instance.InitialRadius));
                return;
            }

            bestCluster.Buildables.Add(buildable);
        }

        private void _changeOwnerAndGroup([NotNull] BaseCluster cluster, ulong newOwner, ulong newGroup)
        {
            foreach (var buildable in cluster.Buildables.ToList())
            {
                Game.ChangeOwnerAndGroup(buildable.Position, newOwner, newGroup);
                buildable.Owner = newOwner;
                buildable.Group = newGroup;
            }
        }

        private void _damage([NotNull] BaseCluster cluster, ushort damage)
        {
            foreach (var buildable in cluster.Buildables.ToList())
                Game.DamageBarricadeStructure(buildable.Position, damage);
        }

        private void _destroyCluster([NotNull] BaseCluster cluster)
        {
            foreach (var buildable in cluster.Buildables.ToList())
            {
                Game.RemoveBarricadeStructure(buildable.Position);
                cluster.Buildables.Remove(buildable);
            }

            Clusters.Remove(cluster);
        }

        private void _repair([NotNull] BaseCluster cluster, float amount, float times)
        {
            foreach (var buildable in cluster.Buildables.ToList())
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
    }
}