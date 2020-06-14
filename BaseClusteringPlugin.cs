using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Classes;
using Pustalorc.Plugins.BaseClustering.API.Statics;
using Pustalorc.Plugins.BaseClustering.Config;
using Pustalorc.Plugins.BaseClustering.Patches;
using Rocket.API.Collections;
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
        public static event OnVoidDelegate OnDataProcessed;

        private Harmony _harmony;

        [NotNull]
        public override TranslationList DefaultTranslations => new TranslationList
        {
            {
                "clusters_regen_warning",
                "WARNING! This operation can take a heavy amount of time! The more buildables and the bigger the auto-radius increment, the longer it will take! Please see console for when it is done."
            },
            {"cannot_be_executed_from_console", "That command cannot be executed from console with those arguments."},
            {"not_available", "N/A"},
            {
                "build_count",
                "There are a total of {0} builds. Specific Item: {1}, Radius: {2}, Player: {3}, Planted Barricades Included: {4}, Filter by Barricades: {5}, Filter by Structures: {6}"
            },
            {"cluster_count", "There are a total of {0} clusters. Specific Item: {1}, Radius: {2}, Player: {3}"},
            {
                "cannot_teleport_no_builds",
                "Cannot teleport anywhere, no buildables found with the following filters. Specific Item: {0}, Player: {1}, Planted Barricades Included: {2}, Filter by Barricades: {3}, Filter by Structures: {4}"
            },
            {
                "cannot_teleport_builds_too_close",
                "Cannot teleport anywhere, all buildables with the specified filters are too close. Specific Item: {0}, Player: {1}, Planted Barricades Included: {2}, Filter by Barricades: {3}, Filter by Structures: {4}"
            },
            {
                "cannot_teleport_no_clusters",
                "Cannot teleport anywhere, no clusters found with the following filters. Player: {0}"
            },
            {"top_builder_format", "At number {0}, {1} [{2}] with {3} buildables!"},
            {"top_cluster_format", "At number {0}, {1} [{2}] with {3} clusters!"},
            {"action_cancelled", "The wreck action was cancelled."},
            {"no_action_queued", "There is no wreck action queued."},
            {"cannot_wreck_no_clusters", "There are no clusters selected, so nothing can be wrecked."},
            {"wrecked_clusters", "Wrecked a total of {0} clusters. Specific Item: {1}, Radius: {2}, Player: {3}"},
            {
                "wreck_clusters_action_queued",
                "Queued a wreck clusters action. Confirm with /wc confirm. Player: {0}, Specific Item: {1}, Radius: {2}, "
            },
            {
                "wreck_clusters_action_queued_new",
                "Discarded previous queued action and queued a new wreck clusters action. Confirm with /wc confirm. Player: {0}, Specific Item: {1}, Radius: {2}, "
            },
            {"cannot_wreck_no_builds", "There are no buildables selected, so nothing can be wrecked."},
            {
                "wrecked",
                "Wrecked a total of {0} buildables. Specific Item: {1}, Radius: {2}, Player: {3}, Planted Barricades Included: {4}, Filter by Barricades: {5}, Filter by Structures: {6}"
            },
            {
                "wreck_action_queued",
                "Queued a wreck action. Confirm with /w confirm. Specific Item: {0}, Radius: {1}, Player: {2}, Planted Barricades Included: {3}, Filter by Barricades: {4}, Filter by Structures: {5}"
            },
            {
                "wreck_action_queued_new",
                "Discarded previous queued action and queued a new wreck action. Confirm with /w confirm. Specific Item: {0}, Radius: {1}, Player: {2}, Planted Barricades Included: {3}, Filter by Barricades: {4}, Filter by Structures: {5}"
            },
            {
                "no_vehicle_found",
                "Couldn't find a vehicle in the direction you're looking, or you are too far away from one. Maximum distance is 10 units."
            },
            {
                "vehicle_dead",
                "The vehicle you are looking at is destroyed and cannot be wrecked. Please look at a vehicle that isn't destroyed."
            },
            {
                "vehicle_no_plant",
                "The vehicle appears to have no assigned barricades to it, please make sure that it has barricades before asking to wreck them."
            },
            {"vehicle_wreck", "Wrecked buildables from {0} [{1}]. Instance ID: {2}, Owner: {3}"}
        };

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
            BarricadeManager.onHarvestPlantRequested -= BarricadeSalvaged;
            BarricadeManager.onTransformRequested -= BarricadeTransformed;
            PatchBarricadeSpawnInternal.OnNewBarricadeSpawned -= BarricadeSpawned;
            PatchBarricadeDestroy.OnBarricadeDestroyed -= BuildableDestroyed;

            StructureManager.onTransformRequested -= StructureTransformed;
            StructureManager.onSalvageStructureRequested -= StructureSalvaged;
            StructureManager.onDamageStructureRequested -= StructureDamaged;
            PatchStructureSpawnInternal.OnNewStructureSpawned -= StructureSpawned;
            PatchStructureDestroy.OnStructureDestroyed -= BuildableDestroyed;

            _harmony.UnpatchAll();
            _harmony = null;

            Instance = null;

            Logging.PluginUnloaded(this);
        }

        private ObservableCollection<BaseCluster> _clusters;

        public ObservableCollection<BaseCluster> Clusters
        {
            get => _clusters;
            private set
            {
                if (_clusters != null)
                    _clusters.CollectionChanged -= ClustersChanged;

                _clusters = value;

                _clusters.CollectionChanged -= ClustersChanged;
                _clusters.CollectionChanged += ClustersChanged;
            }
        }

        [NotNull]
        // ReSharper disable once ReturnTypeCanBeEnumerable.Global
        public IReadOnlyList<Buildable> Buildables =>
            Clusters?.SelectMany(k => k.Buildables).ToList() ?? new List<Buildable>();

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
        ///     Retrieves all clusters that have the player as the most common owner.
        /// </summary>
        /// <param name="player">The player to use for the search as the most common owner.</param>
        /// <returns></returns>
        [NotNull]
        public IEnumerable<BaseCluster> GetMostOwnedClusters(CSteamID player)
        {
            return Clusters?.Where(k => k.CommonOwner == player.m_SteamID) ?? new List<BaseCluster>();
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

        /// <summary>
        ///     Removes a specific buildable from all the clusters where it is found at.
        /// </summary>
        /// <param name="model">The model of the buildable to remove</param>
        public void RemoveBuildable(Transform model)
        {
            var clusters = Clusters.Where(k =>
                k.Buildables.Any(l => l.Model == model));

            foreach (var cluster in clusters.ToList())
            {
                var buildable =
                    cluster.Buildables.FirstOrDefault(l => l.Model == model);

                if (buildable == null)
                {
                    Logging.Verbose(this,
                        "Missed a buildable at some point. Unable to remove from cluster.");
                    continue;
                }

                if (cluster.Buildables.Count == 1)
                {
                    cluster.Buildables.Clear();
                    DestroyCluster(cluster);
                    continue;
                }

                cluster.Buildables.Remove(buildable);
            }
        }

        internal void GenerateAndLoadAllClusters()
        {
            var start = DateTime.Now;

            var allBuildables = ReadOnlyGame.GetBuilds(CSteamID.Nil, false, false).ToList();
            Logging.Write(this,
                $"Total buildables: {allBuildables.Count}. Took {(int) DateTime.Now.Subtract(start).TotalMilliseconds}ms");

            Clusters = Configuration.Instance.ClusteringStyle switch
            {
                EClusteringStyle.Bruteforce => new ObservableCollection<BaseCluster>(
                    Utils.BruteforceClustering(allBuildables, Configuration.Instance.BruteforceOptions)),
                EClusteringStyle.Rust => new ObservableCollection<BaseCluster>(
                    Utils.RustClustering(allBuildables, Configuration.Instance.RustOptions, true)),
                EClusteringStyle.Hybrid => new ObservableCollection<BaseCluster>(Utils.HybridClustering(allBuildables,
                    Configuration.Instance.BruteforceOptions, Configuration.Instance.RustOptions)),
                _ => Clusters
            };

            Logging.Write(this,
                $"Clusters Loaded: {Clusters.Count}. Took {(int) DateTime.Now.Subtract(start).TotalMilliseconds}ms.");
        }

        private void OnLevelLoaded(int level)
        {
            BarricadeManager.onSalvageBarricadeRequested += BarricadeSalvaged;
            BarricadeManager.onDamageBarricadeRequested += BarricadeDamaged;
            BarricadeManager.onHarvestPlantRequested += BarricadeSalvaged;
            BarricadeManager.onTransformRequested += BarricadeTransformed;
            PatchBarricadeSpawnInternal.OnNewBarricadeSpawned += BarricadeSpawned;
            PatchBarricadeDestroy.OnBarricadeDestroyed += BuildableDestroyed;

            StructureManager.onTransformRequested += StructureTransformed;
            StructureManager.onSalvageStructureRequested += StructureSalvaged;
            StructureManager.onDamageStructureRequested += StructureDamaged;
            PatchStructureSpawnInternal.OnNewStructureSpawned += StructureSpawned;
            PatchStructureDestroy.OnStructureDestroyed += BuildableDestroyed;

            GenerateAndLoadAllClusters();
            OnDataProcessed?.Invoke();
        }

        private void BuildableDestroyed(Transform model)
        {
            RemoveBuildable(model);
        }

        private void StructureDamaged(CSteamID instigatorSteamId, Transform structureTransform,
            ref ushort pendingTotalDamage, ref bool shouldAllow, EDamageOrigin damageOrigin)
        {
            if (!shouldAllow || !StructureManager.tryGetInfo(structureTransform, out _, out _, out var index,
                out var region)) return;

            var sData = region.structures[index];
            var sDrop = region.drops.FirstOrDefault(k => k.instanceID == sData.instanceID);

            if (sData.structure.isDead)
            {
                RemoveBuildable(sDrop.model);
                return;
            }

            if (pendingTotalDamage < 1 || pendingTotalDamage < sData.structure.health) return;

            RemoveBuildable(sDrop.model);
        }

        private void StructureSpawned([NotNull] StructureData data, [NotNull] StructureDrop drop)
        {
            var buildable = new Buildable(data.angle_x, data.angle_y, data.angle_z, data.structure.id,
                data.structure.health, data.instanceID, data.owner, data.group, data.point, data.structure.asset,
                drop.model, null, null);

            var bestCluster = Clusters.FindBestCluster(buildable, Configuration.Instance.MaxClusterSelfExpandRadius);

            if (bestCluster == null)
            {
                var config = Configuration.Instance;

                switch (config.ClusteringStyle)
                {
                    case EClusteringStyle.Bruteforce:
                        Clusters.Add(new BaseCluster(new List<Buildable> {buildable}, buildable.Position,
                            config.BruteforceOptions.InitialRadius, false));
                        break;
                    case EClusteringStyle.Rust:
                        var globalCluster = Clusters.FirstOrDefault(k => k.IsGlobalCluster);
                        if (globalCluster != null) globalCluster.Buildables.Add(buildable);
                        else
                            Clusters.Add(new BaseCluster(new List<Buildable> {buildable}, buildable.Position,
                                1.73205078f + config.RustOptions.ExtraRadius, true));
                        break;
                    case EClusteringStyle.Hybrid:
                        Clusters.Add(new BaseCluster(new List<Buildable> {buildable}, buildable.Position,
                            config.RustOptions.FloorIds.Contains(buildable.AssetId)
                                ? 1.73205078f + config.RustOptions.ExtraRadius
                                : config.BruteforceOptions.InitialRadius, false));
                        break;
                }

                return;
            }

            bestCluster.Buildables.Add(buildable);
        }

        private void StructureSalvaged(CSteamID steamId, byte x, byte y, ushort index, ref bool shouldAllow)
        {
            if (!shouldAllow || !StructureManager.tryGetRegion(x, y, out var region)) return;

            RemoveBuildable(region.drops.FirstOrDefault(k => k.instanceID == region.structures[index].instanceID)
                .model);
        }

        private void StructureTransformed(CSteamID instigator, byte x, byte y, uint instanceId, ref Vector3 point,
            ref byte angleX, ref byte angleY, ref byte angleZ, ref bool shouldAllow)
        {
            if (!shouldAllow) return;

            var cluster = Clusters.FirstOrDefault(k => k.Buildables.Any(l => l.InstanceId == instanceId));
            if (cluster == null)
            {
                Logging.Verbose(this, $"Missed a barricade being added with instance ID {instanceId}");
                return;
            }

            var buildable = cluster.Buildables.FirstOrDefault(k => k.InstanceId == instanceId);
            cluster.Buildables.Remove(buildable);

            if (cluster.Buildables.Count == 0)
                DestroyCluster(cluster);

            // ReSharper disable once PossibleNullReferenceException
            buildable.AngleX = angleX;
            buildable.AngleY = angleY;
            buildable.AngleZ = angleZ;
            buildable.Position = point;

            var bestCluster = Clusters.FindBestCluster(buildable, Configuration.Instance.MaxClusterSelfExpandRadius);

            if (bestCluster == null)
            {
                var config = Configuration.Instance;

                switch (config.ClusteringStyle)
                {
                    case EClusteringStyle.Bruteforce:
                        Clusters.Add(new BaseCluster(new List<Buildable> {buildable}, buildable.Position,
                            config.BruteforceOptions.InitialRadius, false));
                        break;
                    case EClusteringStyle.Rust:
                        var globalCluster = Clusters.FirstOrDefault(k => k.IsGlobalCluster);
                        if (globalCluster != null) globalCluster.Buildables.Add(buildable);
                        else
                            Clusters.Add(new BaseCluster(new List<Buildable> {buildable}, buildable.Position,
                                1.73205078f + config.RustOptions.ExtraRadius, true));
                        break;
                    case EClusteringStyle.Hybrid:
                        Clusters.Add(new BaseCluster(new List<Buildable> {buildable}, buildable.Position,
                            config.RustOptions.FloorIds.Contains(buildable.AssetId)
                                ? 1.73205078f + config.RustOptions.ExtraRadius
                                : config.BruteforceOptions.InitialRadius, false));
                        break;
                }

                return;
            }

            bestCluster.Buildables.Add(buildable);
        }

        private void BarricadeDamaged(CSteamID instigatorSteamId, Transform barricadeTransform,
            ref ushort pendingTotalDamage, ref bool shouldAllow, EDamageOrigin damageOrigin)
        {
            if (!shouldAllow || !BarricadeManager.tryGetInfo(barricadeTransform, out _, out _, out _,
                out var index, out var region)) return;

            var bData = region.barricades[index];
            var bDrop = region.drops.FirstOrDefault(k => k.instanceID == bData.instanceID);

            if (bData.barricade.isDead)
            {
                RemoveBuildable(bDrop.model);
                return;
            }

            if (pendingTotalDamage < 1 || pendingTotalDamage < bData.barricade.health) return;

            RemoveBuildable(bDrop.model);
        }

        private void BarricadeSpawned([NotNull] BarricadeData data, [NotNull] BarricadeDrop drop)
        {
            var buildable = new Buildable(data.angle_x, data.angle_y, data.angle_z, data.barricade.id,
                data.barricade.health, data.instanceID, data.owner, data.group, data.point, drop.asset, drop.model,
                drop.interactable, data.barricade.state);

            var bestCluster = Clusters.FindBestCluster(buildable, Configuration.Instance.MaxClusterSelfExpandRadius);

            if (bestCluster == null)
            {
                var config = Configuration.Instance;

                switch (config.ClusteringStyle)
                {
                    case EClusteringStyle.Bruteforce:
                        Clusters.Add(new BaseCluster(new List<Buildable> {buildable}, buildable.Position,
                            config.BruteforceOptions.InitialRadius, false));
                        break;
                    case EClusteringStyle.Rust:
                        var globalCluster = Clusters.FirstOrDefault(k => k.IsGlobalCluster);
                        if (globalCluster != null) globalCluster.Buildables.Add(buildable);
                        else
                            Clusters.Add(new BaseCluster(new List<Buildable> {buildable}, buildable.Position,
                                1.73205078f + config.RustOptions.ExtraRadius, true));
                        break;
                    case EClusteringStyle.Hybrid:
                        Clusters.Add(new BaseCluster(new List<Buildable> {buildable}, buildable.Position,
                            config.RustOptions.FloorIds.Contains(buildable.AssetId)
                                ? 1.73205078f + config.RustOptions.ExtraRadius
                                : config.BruteforceOptions.InitialRadius, false));
                        break;
                }

                return;
            }

            bestCluster.Buildables.Add(buildable);
        }

        private void BarricadeSalvaged(CSteamID steamId, byte x, byte y, ushort plant, ushort index,
            ref bool shouldAllow)
        {
            if (!shouldAllow || !BarricadeManager.tryGetRegion(x, y, plant, out var region)) return;

            RemoveBuildable(region.drops.FirstOrDefault(k => k.instanceID == region.barricades[index].instanceID)
                .model);
        }

        private void BarricadeTransformed(CSteamID instigator, byte x, byte y, ushort plant, uint instanceId,
            ref Vector3 point, ref byte angleX, ref byte angleY, ref byte angleZ, ref bool shouldAllow)
        {
            if (!shouldAllow) return;

            var cluster = Clusters.FirstOrDefault(k => k.Buildables.Any(l => l.InstanceId == instanceId));
            if (cluster == null)
            {
                Logging.Verbose(this, $"Missed a barricade being added with instance ID {instanceId}");
                return;
            }

            var buildable = cluster.Buildables.FirstOrDefault(k => k.InstanceId == instanceId);
            cluster.Buildables.Remove(buildable);

            if (cluster.Buildables.Count == 0)
                DestroyCluster(cluster);

            // ReSharper disable once PossibleNullReferenceException
            buildable.AngleX = angleX;
            buildable.AngleY = angleY;
            buildable.AngleZ = angleZ;
            buildable.Position = point;

            var bestCluster = Clusters.FindBestCluster(buildable, Configuration.Instance.MaxClusterSelfExpandRadius);

            if (bestCluster == null)
            {
                var config = Configuration.Instance;

                switch (config.ClusteringStyle)
                {
                    case EClusteringStyle.Bruteforce:
                        Clusters.Add(new BaseCluster(new List<Buildable> {buildable}, buildable.Position,
                            config.BruteforceOptions.InitialRadius, false));
                        break;
                    case EClusteringStyle.Rust:
                        var globalCluster = Clusters.FirstOrDefault(k => k.IsGlobalCluster);
                        if (globalCluster != null) globalCluster.Buildables.Add(buildable);
                        else
                            Clusters.Add(new BaseCluster(new List<Buildable> {buildable}, buildable.Position,
                                1.73205078f + config.RustOptions.ExtraRadius, true));
                        break;
                    case EClusteringStyle.Hybrid:
                        Clusters.Add(new BaseCluster(new List<Buildable> {buildable}, buildable.Position,
                            config.RustOptions.FloorIds.Contains(buildable.AssetId)
                                ? 1.73205078f + config.RustOptions.ExtraRadius
                                : config.BruteforceOptions.InitialRadius, false));
                        break;
                }

                return;
            }

            bestCluster.Buildables.Add(buildable);
        }

        private void _changeOwnerAndGroup([NotNull] BaseCluster cluster, ulong newOwner, ulong newGroup)
        {
            foreach (var buildable in cluster.Buildables.ToList())
            {
                WriteOnlyGame.ChangeOwnerAndGroup(buildable.Position, newOwner, newGroup);
                buildable.Owner = newOwner;
                buildable.Group = newGroup;
            }
        }

        private void _damage([NotNull] BaseCluster cluster, ushort damage)
        {
            foreach (var buildable in cluster.Buildables.ToList())
                WriteOnlyGame.DamageBarricadeStructure(buildable.Position, damage);
        }

        private void _destroyCluster([NotNull] BaseCluster cluster)
        {
            foreach (var buildable in cluster.Buildables.ToList())
            {
                WriteOnlyGame.RemoveBarricadeStructure(buildable.Position);
                cluster.Buildables.Remove(buildable);
            }

            Clusters.Remove(cluster);
        }

        private void _repair([NotNull] BaseCluster cluster, float amount, float times)
        {
            foreach (var buildable in cluster.Buildables.ToList())
                WriteOnlyGame.RepairBarricadeStructure(buildable.Position, amount, times);
        }

        private void _removeAllClusters()
        {
            foreach (var cluster in Clusters.ToList())
            {
                foreach (var buildable in cluster.Buildables.ToList())
                {
                    WriteOnlyGame.RemoveBarricadeStructure(buildable.Position);
                    cluster.Buildables.Remove(buildable);
                }

                Clusters.Remove(cluster);
            }
        }

        private void ClustersChanged(object sender, [NotNull] NotifyCollectionChangedEventArgs e)
        {
            Logging.Verbose(this, $"Clusters were modified. Action: {e.Action}");
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var cluster in e.NewItems.Cast<BaseCluster>())
                        Logging.Verbose(this,
                            $"New cluster created at: {cluster.CenterBuildable}\nRadius: {cluster.Radius}\nAverage Center: {cluster.AverageCenterPosition}\nMost common group: {cluster.CommonGroup}\nMost common owner: {cluster.CommonOwner}\nAll buildables: {string.Join(", ", cluster.Buildables.Select(k => k.Position))}");
                    break;
                case NotifyCollectionChangedAction.Move:
                    Logging.Verbose(this,
                        $"Cluster moved from index {e.OldStartingIndex} to index {e.NewStartingIndex}");
                    break;
                case NotifyCollectionChangedAction.Remove:
                    Logging.Verbose(this, $"Cluster removed at index {e.OldStartingIndex}");
                    break;
                case NotifyCollectionChangedAction.Replace:
                    Logging.Verbose(this, $"Cluster replaced at index {e.OldStartingIndex}");
                    break;
                case NotifyCollectionChangedAction.Reset:
                    Logging.Verbose(this, "Clusters collection was reset. Most likely a Clear() call.");
                    break;
            }
        }
    }
}