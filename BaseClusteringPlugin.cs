using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
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

        public delegate void OnClustersChanged(BaseCluster cluster);

        public static event OnClustersChanged OnClusterAdded;
        public static event OnClustersChanged OnClusterRemoved;
        public static event OnVoidDelegate OnClustersCleared;

        private ulong m_InstanceCount;

        public ulong InstanceCount
        {
            get => m_InstanceCount;
            set => m_InstanceCount = value;
        }

        private Harmony m_Harmony;

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
            {"vehicle_wreck", "Wrecked buildables from {0} [{1}]. Instance ID: {2}, Owner: {3}"},
            {"not_enough_args", "You need more arguments to use this command."}
        };

        protected override void Load()
        {
            Instance = this;
            m_Harmony = new Harmony("xyz.pustalorc.baseClustering");
            m_Harmony.PatchAll();

            if (Level.isLoaded)
                OnLevelLoaded(0);
            else
                Level.onLevelLoaded += OnLevelLoaded;

            Logging.PluginLoaded(this);
        }

        protected override void Unload()
        {
            PatchBuildableTransforms.OnBuildableTransformed -= BuildableTransformed;
            PatchBuildableSpawns.OnBuildableSpawned -= BuildableSpawned;
            PatchBuildablesDestroy.OnBuildableDestroyed -= BuildableDestroyed;
            Provider.onCommenceShutdown -= Save;
            SaveManager.onPreSave -= Save;

            m_Harmony.UnpatchAll();
            m_Harmony = null;

            Instance = null;

            Logging.PluginUnloaded(this);
        }

        private ObservableCollection<BaseCluster> m_Clusters;

        public ObservableCollection<BaseCluster> Clusters
        {
            get => m_Clusters;
            private set
            {
                if (m_Clusters == null)
                {
                    m_Clusters = value;
                    m_Clusters.CollectionChanged += ClustersChanged;
                    return;
                }

                m_Clusters.Clear();
                foreach (var cluster in value)
                    m_Clusters.Add(cluster);
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
        /// <param name="sqrRadius">The maximum distance (raised to the power of 2) to detect a cluster in.</param>
        /// <returns></returns>
        [NotNull]
        public IEnumerable<BaseCluster> GetClustersInRadius(Vector3 center, float sqrRadius)
        {
            return Clusters?.Where(k => (k.CenterBuildable - center).sqrMagnitude < sqrRadius) ?? new List<BaseCluster>();
        }

        /// <summary>
        ///     Retrieves all clusters that have the player as the most common owner.
        /// </summary>
        /// <param name="player">The player to use for the search as the most common owner.</param>
        /// <returns></returns>
        [NotNull]
        public IEnumerable<BaseCluster> GetMostOwnedClusters(CSteamID player)
        {
            return GetClustersWithFilter(k => k.CommonOwner == player.m_SteamID);
        }

        /// <summary>
        ///     Retrieves all clusters that satisfy the custom filter.
        /// </summary>
        /// <param name="filter">An anonymous function that takes BaseCluster as parameter and returns bool.</param>
        /// <returns>A list of clusters that satisfy the filter.</returns>
        [NotNull]
        public IEnumerable<BaseCluster> GetClustersWithFilter(Func<BaseCluster, bool> filter)
        {
            return Clusters?.Where(filter) ?? new List<BaseCluster>();
        }

        /// <summary>
        ///     Gets the cluster that contains the element with the provided position.
        /// </summary>
        /// <param name="position">The position of the buildable within a cluster.</param>
        /// <returns></returns>
        [CanBeNull]
        public BaseCluster GetClusterWithElement(Vector3 position)
        {
            return Clusters?.FirstOrDefault(k => k.Buildables.Any(l => l.Position == position));
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
            RemoveBuildableWithAffected(model);
        }

        /// <summary>
        ///     Removes a specific buildable from all the clusters where it is found at.
        /// </summary>
        /// <param name="model">The model of the buildable to remove</param>
        /// <returns>The list of clusters modified.</returns>
        [NotNull]
        public List<BaseCluster> RemoveBuildableWithAffected(Transform model)
        {
            var result = new List<BaseCluster>();
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
                result.Add(cluster);
            }

            return result;
        }

        private void OnLevelLoaded(int level)
        {
            PatchBuildableTransforms.OnBuildableTransformed += BuildableTransformed;
            PatchBuildableSpawns.OnBuildableSpawned += BuildableSpawned;
            PatchBuildablesDestroy.OnBuildableDestroyed += BuildableDestroyed;
            Provider.onCommenceShutdown += Save;
            SaveManager.onPreSave += Save;

            GenerateAndLoadAllClusters();
            OnDataProcessed?.Invoke();
        }

        internal void GenerateAndLoadAllClusters()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var allBuildables = ReadOnlyGame.GetBuilds(usePreProcessedData: false).ToList();
            Logging.Write(this,
                $"Total buildables: {allBuildables.Count}. Took {stopwatch.ElapsedMilliseconds}ms");

            if (!LevelSavedata.fileExists("/Bases.dat") || !Load(allBuildables))
            {
                Clusters = Configuration.Instance.ClusteringStyle switch
                {
                    EClusteringStyle.Bruteforce => new ObservableCollection<BaseCluster>(
                        Utils.BruteforceClustering(allBuildables, Configuration.Instance.BruteforceOptions,
                            ref m_InstanceCount)),
                    EClusteringStyle.Rust => new ObservableCollection<BaseCluster>(Utils.RustClustering(allBuildables,
                        Configuration.Instance.RustOptions, true, ref m_InstanceCount)),
                    EClusteringStyle.Hybrid => new ObservableCollection<BaseCluster>(Utils.HybridClustering(allBuildables,
                        Configuration.Instance.BruteforceOptions, Configuration.Instance.RustOptions, ref m_InstanceCount)),
                    _ => Clusters
                };
            }

            stopwatch.Stop();
            Logging.Write(this,
                $"Clusters Loaded: {Clusters.Count}. Took {stopwatch.ElapsedMilliseconds}ms.");
        }

        private bool Load(List<Buildable> allBuildables)
        {
            var bases = new List<BaseCluster>();
            var river = LevelSavedata.openRiver("/Bases.dat", true);
         
            var style = (EClusteringStyle)river.readByte();

            if (style != Configuration.Instance.ClusteringStyle)
            {
                Logging.Write(this, "WARNING! CLUSTERING STYLE WAS CHANGED DURING SERVER DOWNTIME! CLUSTER REGEN IS REQUIRED. WILL NOT KEEP LOADING SAVE DATA.", ConsoleColor.Yellow);
                river.closeRiver();
                return false;
            }

            var defaultRadius = river.readSingle();
            InstanceCount = river.readUInt64();
            var clusterCount = river.readInt32();

            for (var i = 0; i < clusterCount; i++)
            {
                var builds = new List<Buildable>();
                var instanceId = river.readUInt64();
                var global = river.readBoolean();
                var radius = double.TryParse(river.readString(), out var rad) ? rad : defaultRadius;

                var buildCount = river.readInt32();
                for (var o = 0; o < buildCount; o++)
                {
                    var buildInstanceId = river.readUInt32();
                    var build = allBuildables.Find(k => k.InstanceId == buildInstanceId);

                    if (build == null)
                    {
                        Logging.Write(this, $"WARNING! BUILDABLE SAVE DATA WAS MODIFIED DURING SERVER DOWNTIME! MISSING BUILDABLE WITH INSTANCE ID {instanceId}. CLUSTER REGEN IS REQUIRED. WILL NOT KEEP LOADING SAVE DATA.", ConsoleColor.Yellow);
                        river.closeRiver();
                        return false;
                    }

                    builds.Add(build);
                }

                var centerBuild = builds.ElementAt(builds.GetCenterIndex()).Position;
                bases.Add(new BaseCluster(builds, builds.ElementAt(builds.GetCenterIndex()).Position, radius, global, instanceId));
            }

            Clusters = new ObservableCollection<BaseCluster>(bases);
            return true;
        }

        public void Save()
        {
            var river = LevelSavedata.openRiver("/Bases.dat", false);
            river.writeByte((byte)Configuration.Instance.ClusteringStyle);
            switch (Configuration.Instance.ClusteringStyle)
            {
                case EClusteringStyle.Bruteforce:
                    river.writeSingle(Configuration.Instance.BruteforceOptions.InitialRadius);
                    break;
                case EClusteringStyle.Rust:
                    river.writeSingle(6.1f + Configuration.Instance.RustOptions.ExtraRadius);
                    break;
                case EClusteringStyle.Hybrid:
                    river.writeSingle(0);
                    break;
            }
            river.writeUInt64(InstanceCount);
            river.writeInt32(Clusters.Count);
            foreach (var cluster in Clusters)
            {
                river.writeUInt64(cluster.InstanceId);
                river.writeBoolean(cluster.IsGlobalCluster);
                river.writeString(cluster.Radius.ToString());
                river.writeInt32(cluster.Buildables.Count);
                foreach (var build in cluster.Buildables)
                    river.writeUInt32(build.InstanceId);
            }
            river.closeRiver();
        }

        private void BuildableDestroyed(Transform model)
        {
            var affected = RemoveBuildableWithAffected(model);

            foreach (var cluster in affected)
            {
                var clusterRegened = Utils.HybridClustering(cluster.Buildables.ToList(),
                    Configuration.Instance.BruteforceOptions, Configuration.Instance.RustOptions, ref m_InstanceCount);

                if (clusterRegened.Count <= 1) continue;

                Clusters.Remove(cluster);

                foreach (var c in clusterRegened)
                    Clusters.Add(c);
            }
        }

        private void BuildableTransformed(uint instanceId)
        {
            var config = Configuration.Instance;
            var cluster = Clusters.FirstOrDefault(k => k.Buildables.Any(l => l.InstanceId == instanceId));
            if (cluster == null)
            {
                Logging.Verbose(this, $"Missed a barricade being added with instance ID {instanceId}");
                return;
            }

            var buildable = cluster.Buildables.First(k => k.InstanceId == instanceId);
            cluster.Buildables.Remove(buildable);

            if (cluster.Buildables.Count == 0)
                DestroyCluster(cluster);

            var bestCluster = config.ClusteringStyle switch
            {
                EClusteringStyle.Bruteforce => Clusters.FindBestClusterWithMaxDistance(buildable,
                    config.BruteforceOptions.MaxRadius),
                _ => Clusters.FindBestCluster(buildable, config.RustOptions.ExtraRadius)
            };

            if (bestCluster == null)
            {
                switch (config.ClusteringStyle)
                {
                    case EClusteringStyle.Bruteforce:
                        Clusters.Add(new BaseCluster(new List<Buildable> {buildable}, buildable.Position,
                            config.BruteforceOptions.InitialRadius, false, InstanceCount++));
                        break;
                    case EClusteringStyle.Rust:
                        var globalCluster = Clusters.FirstOrDefault(k => k.IsGlobalCluster);
                        if (globalCluster != null) globalCluster.Buildables.Add(buildable);
                        else
                            Clusters.Add(new BaseCluster(new List<Buildable> {buildable}, buildable.Position,
                                6.1f + config.RustOptions.ExtraRadius, true, InstanceCount++));
                        break;
                    case EClusteringStyle.Hybrid:
                        Clusters.Add(new BaseCluster(new List<Buildable> {buildable}, buildable.Position,
                            config.RustOptions.FloorIds.Contains(buildable.AssetId)
                                ? 6.1f + config.RustOptions.ExtraRadius
                                : config.BruteforceOptions.InitialRadius, false, InstanceCount++));
                        break;
                }

                return;
            }

            bestCluster.Buildables.Add(buildable);
        }

        private void BuildableSpawned([NotNull] Buildable buildable)
        {
            var config = Configuration.Instance;

            var bestClusters = config.ClusteringStyle switch
            {
                EClusteringStyle.Bruteforce => Clusters.FindBestClustersWithMaxDistance(buildable,
                    config.BruteforceOptions.MaxRadius),
                _ => Clusters.FindBestClusters(buildable, config.RustOptions.ExtraRadius)
            };

            var clusterCount = bestClusters.Count();
            if (clusterCount == 0)
            {
                switch (config.ClusteringStyle)
                {
                    case EClusteringStyle.Bruteforce:
                        Clusters.Add(new BaseCluster(new List<Buildable> {buildable}, buildable.Position,
                            config.BruteforceOptions.InitialRadius, false, InstanceCount++));
                        break;
                    case EClusteringStyle.Rust:
                        var globalCluster = Clusters.FirstOrDefault(k => k.IsGlobalCluster);
                        if (globalCluster != null) globalCluster.Buildables.Add(buildable);
                        else
                            Clusters.Add(new BaseCluster(new List<Buildable> {buildable}, buildable.Position,
                                6.1f + config.RustOptions.ExtraRadius, true, InstanceCount++));
                        break;
                    case EClusteringStyle.Hybrid:
                        Clusters.Add(new BaseCluster(new List<Buildable> {buildable}, buildable.Position,
                            config.RustOptions.FloorIds.Contains(buildable.AssetId)
                                ? 6.1f + config.RustOptions.ExtraRadius
                                : config.BruteforceOptions.InitialRadius, false, InstanceCount++));
                        break;
                }

                return;
            }

            if (clusterCount > 1)
            {
                var allBuilds = bestClusters.SelectMany(k => k.Buildables).ToList();

                var newClusters = Utils.HybridClustering(allBuilds, Configuration.Instance.BruteforceOptions,
                    Configuration.Instance.RustOptions, ref m_InstanceCount);

                foreach (var cluster in bestClusters)
                    Clusters.Remove(cluster);

                foreach (var cluster in newClusters)
                    Clusters.Add(cluster);

                return;
            }

            bestClusters.First().Buildables.Add(buildable);
        }

        private void _changeOwnerAndGroup([NotNull] BaseCluster cluster, ulong newOwner, ulong newGroup)
        {
            foreach (var buildable in cluster.Buildables.ToList())
                WriteOnlyGame.ChangeOwnerAndGroup(buildable.Position, newOwner, newGroup);
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
                    // Raise an event for every single item added.
                    foreach (var cluster in e.NewItems.Cast<BaseCluster>())
                    {
                        Logging.Verbose(this,
                            $"New cluster created at: {cluster.CenterBuildable}\nRadius: {cluster.Radius}\nAverage Center: {cluster.AverageCenterPosition}\nMost common group: {cluster.CommonGroup}\nMost common owner: {cluster.CommonOwner}\nAll buildables: {string.Join(", ", cluster.Buildables.Select(k => k.Position))}");
                        OnClusterAdded?.Invoke(cluster);
                    }

                    break;
                case NotifyCollectionChangedAction.Move:
                    // No event needed, it was just switched in the position of the list.
                    Logging.Verbose(this,
                        $"Cluster moved from index {e.OldStartingIndex} to index {e.NewStartingIndex}");
                    break;
                case NotifyCollectionChangedAction.Remove:
                    // Raise an event for every single item removed.
                    Logging.Verbose(this, $"Cluster removed at index {e.OldStartingIndex}");
                    foreach (var cluster in e.OldItems.Cast<BaseCluster>())
                        OnClusterRemoved?.Invoke(cluster);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    // Raise an event for the cluster that was replaced (removed).
                    Logging.Verbose(this, $"Cluster replaced at index {e.OldStartingIndex}");
                    foreach (var cluster in e.OldItems.Cast<BaseCluster>())
                        OnClusterRemoved?.Invoke(cluster);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    Logging.Verbose(this, "Clusters collection was reset. Most likely a Clear() call.");
                    OnClustersCleared?.Invoke();
                    break;
            }
        }
    }
}