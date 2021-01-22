using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API;
using Pustalorc.Plugins.BaseClustering.API.Buildables;
using Pustalorc.Plugins.BaseClustering.API.Delegates;
using Pustalorc.Plugins.BaseClustering.API.Patches;
using Pustalorc.Plugins.BaseClustering.API.Utils;
using Pustalorc.Plugins.BaseClustering.Config;
using Rocket.API.Collections;
using Rocket.Core.Plugins;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

// ReSharper disable MemberCanBeMadeStatic.Local

namespace Pustalorc.Plugins.BaseClustering
{
    public sealed class BaseClusteringPlugin : RocketPlugin<BaseClusteringPluginConfiguration>
    {
        public static BaseClusteringPlugin Instance { get; private set; }
        public static event OnVoidDelegate OnBuildablesProcessed;
        public event OnVoidDelegate OnClustersCleared;
        public event OnClustersChanged OnClusterAdded;
        public event OnClustersChanged OnClusterRemoved;

        private ObservableCollection<BaseCluster> m_Clusters;
        private Queue<int> m_FreeInstanceIds;
        private int m_InstanceCount;
        private readonly object m_InstanceCountLock = new object();
        private Harmony m_Harmony;

        public ClusteringTool ClusteringTool { get; private set; }

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
            {"top_builder_format", "At number {0}, {1} with {2} buildables!"},
            {"top_cluster_format", "At number {0}, {1} with {2} clusters!"},
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
            ClusteringTool = new ClusteringTool(this);
            m_Harmony = new Harmony("xyz.pustalorc.baseClustering");
            m_Harmony.PatchAll();
            lock (m_FreeInstanceIds)
            {
                m_FreeInstanceIds = new Queue<int>();
            }

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
            Provider.onCommenceShutdown -= ForceDataSave;
            SaveManager.onPostSave -= Save;

            m_Harmony.UnpatchAll();
            m_Harmony = null;

            Instance = null;

            Logging.PluginUnloaded(this);
        }

        private void OnLevelLoaded(int level)
        {
            PatchBuildableTransforms.OnBuildableTransformed += BuildableTransformed;
            PatchBuildableSpawns.OnBuildableSpawned += BuildableSpawned;
            PatchBuildablesDestroy.OnBuildableDestroyed += BuildableDestroyed;
            Provider.onCommenceShutdown += ForceDataSave;
            SaveManager.onPostSave += Save;

            GenerateAndLoadAllClusters();
            OnBuildablesProcessed?.Invoke();
        }

        internal void GenerateAndLoadAllClusters()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var allBuildables = BuildableCollection.GetBuildables(usePreProcessedData: false);
            Logging.Write(this,
                $"Loaded {allBuildables.Count()} buildables in map from scratch. Took {stopwatch.ElapsedMilliseconds}ms");

            if (!LevelSavedata.fileExists("/Bases.dat") || !Load(allBuildables))
                Clusters = new ObservableCollection<BaseCluster>(
                    ClusteringTool.ClusterElements(allBuildables));

            stopwatch.Stop();
            Logging.Write(this,
                $"Clusters Loaded: {Clusters.Count}. Took {stopwatch.ElapsedMilliseconds}ms.");
        }

        private bool Load(IEnumerable<Buildable> allBuildables)
        {
            lock (m_InstanceCountLock)
            {
                lock (m_FreeInstanceIds)
                {
                    try
                    {
                        var bases = new List<BaseCluster>();
                        var river = new RiverExpanded(ServerSavedata.directory + "/" + Provider.serverID + "/Level/" +
                                                      Level.info.name + "/Bases.dat");

                        var clusterCount = river.ReadInt32();

                        for (var i = 0; i < clusterCount; i++)
                        {
                            var builds = new List<Buildable>();
                            // Restore of instanceId is needed.
                            var instanceId = river.ReadInt32();
                            var global = river.ReadBoolean();

                            var buildCount = river.ReadInt32();
                            for (var o = 0; o < buildCount; o++)
                            {
                                var buildInstanceId = river.ReadUInt32();
                                var isStructure = river.ReadBoolean();
                                var build = allBuildables.FirstOrDefault(k =>
                                    k.InstanceId == buildInstanceId && k is StructureBuildable == isStructure);

                                if (build == null)
                                {
                                    Logging.Write(this,
                                        $"Warning! Buildable with InstanceId {buildInstanceId} not found! Save data was most likely modified or lost during server downtime. Clusters will be now rebuilt.",
                                        ConsoleColor.Yellow);
                                    river.CloseRiver();
                                    return false;
                                }

                                builds.Add(build);
                            }

                            bases.Add(new BaseCluster(this, instanceId, global, builds));
                        }

                        Clusters = new ObservableCollection<BaseCluster>(bases);

                        m_InstanceCount = Clusters.Max(k => k.InstanceId) + 1;

                        for (var i = 0; i < m_InstanceCount; i++)
                        {
                            if (Clusters.Any(k => k.InstanceId == i))
                                continue;

                            m_FreeInstanceIds.Enqueue(i);
                        }

                        return true;
                    }
                    catch (Exception ex)
                    {
                        Logging.Write(this,
                            $"Warning! An exception was thrown when attempting to load the file. Assuming the data is corrupted. Clusters will be now rebuilt. Exception: {ex}",
                            ConsoleColor.Yellow);
                        m_InstanceCount = 0;
                        m_FreeInstanceIds.Clear();
                        return false;
                    }
                }
            }
        }

        private void ForceDataSave()
        {
            SaveManager.save();
        }

        public void Save()
        {
            var river = new RiverExpanded(ServerSavedata.directory + "/" + Provider.serverID + "/Level/" +
                                          Level.info.name + "/Bases.dat");
            river.WriteInt32(Clusters.Count);
            foreach (var cluster in Clusters)
            {
                river.WriteInt32(cluster.InstanceId);
                river.WriteBoolean(cluster.IsGlobalCluster);
                river.WriteInt32(cluster.Buildables.Count);
                foreach (var build in cluster.Buildables)
                {
                    river.WriteBoolean(build is StructureBuildable);
                    river.WriteUInt32(build.InstanceId);
                }
            }

            river.CloseRiver();
        }

        public int GetBestInstanceId()
        {
            lock (m_InstanceCountLock)
            {
                lock (m_FreeInstanceIds)
                {
                    return m_FreeInstanceIds.Any() ? m_FreeInstanceIds.Dequeue() : m_InstanceCount++;
                }
            }
        }

        [NotNull]
        // ReSharper disable once ReturnTypeCanBeEnumerable.Global
        // The purpose of this is to give a non-modifiable post processed buildable list. IEnumerable won't suffice.
        // IReadonly is FORCED on purpose.
        public IReadOnlyCollection<Buildable> PostProcessedBuildables => Clusters?.SelectMany(k => k.Buildables).ToList() ?? new List<Buildable>();

        /// <summary>
        /// Retrieves all clusters that have the player as the most common owner.
        /// </summary>
        /// <param name="player">The player to use for the search as the most common owner.</param>
        /// <returns>An IEnumerable holding all the clusters that this player is deemed "most common owner" of.</returns>
        [NotNull]
        public IEnumerable<BaseCluster> GetMostOwnedClusters(CSteamID player)
        {
            return GetClustersWithFilter(k => k.CommonOwner == player.m_SteamID);
        }

        /// <summary>
        /// Retrieves all clusters that satisfy the custom filter.
        /// </summary>
        /// <param name="filter">An anonymous function that takes BaseCluster as parameter and returns bool.</param>
        /// <returns>A list of clusters that satisfy the filter.</returns>
        [NotNull]
        public IEnumerable<BaseCluster> GetClustersWithFilter(Func<BaseCluster, bool> filter)
        {
            return Clusters?.Where(filter) ?? new List<BaseCluster>();
        }

        /// <summary>
        /// Gets the cluster that contains the element with the provided model.
        /// </summary>
        /// <param name="model">The model of the buildable within a cluster.</param>
        /// <returns>The cluster in which the model is located within. Null if no cluster is found.</returns>
        [CanBeNull]
        public BaseCluster GetClusterWithElement(Transform model)
        {
            return Clusters?.FirstOrDefault(k => k.Buildables.Any(l => l.Model == model));
        }

        /// <summary>
        /// Gets the cluster that contains the element with the provided position.
        /// </summary>
        /// <param name="instanceId">The instanceId of the buildable within a cluster.</param>
        /// <param name="isStructure">If the instanceId belongs to a structure or a barricade.</param>
        /// <returns>The cluster containing the element provided.</returns>
        [CanBeNull]
        public BaseCluster GetClusterWithElement(uint instanceId, bool isStructure)
        {
            return Clusters?.FirstOrDefault(k =>
            {
                var builds = k.Buildables.AsEnumerable();

                if (builds == null)
                    return false;

                if (isStructure)
                    builds = builds.OfType<StructureBuildable>();
                else
                    builds = builds.OfType<BarricadeBuildable>();

                return builds.Any(l => l.InstanceId == instanceId);
            });
        }

        /// <summary>
        /// Gets the cluster that contains the element with the provided buildable instance.
        /// </summary>
        /// <param name="buildable">The buildable within a cluster.</param>
        /// <returns>The cluster this buildable belongs to.</returns>
        [CanBeNull]
        public BaseCluster GetClusterWithElement(Buildable buildable)
        {
            return Clusters?.FirstOrDefault(k => k.Buildables.Contains(buildable));
        }

        /// <summary>
        /// Removes a specific buildable from all the clusters where it is found at.
        /// </summary>
        /// <param name="model">The model of the buildable to remove.</param>
        /// <returns>The list of clusters modified.</returns>
        [NotNull]
        public IEnumerable<BaseCluster> RemoveBuildableWithAffected(Transform model)
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

                cluster.Buildables.Remove(buildable);
                result.Add(cluster);
            }

            return result;
        }

        // Patched event handling
        private void BuildableDestroyed(Transform model)
        {
            var affected = RemoveBuildableWithAffected(model);

            foreach (var cluster in affected)
            {
                var clusterRegened = ClusteringTool.ClusterElements(cluster.Buildables.ToList());

                if (clusterRegened.Count() <= 1) continue;

                UntrackCluster(cluster);

                foreach (var c in clusterRegened)
                    Clusters.Add(c);
            }
        }

        private void BuildableTransformed(uint instanceId, bool isStructure)
        {
            var cluster = GetClusterWithElement(instanceId, isStructure);
            Buildable buildable;

            if (cluster == null)
            {
                Logging.Verbose(this, $"Missed a buildable being added with instance ID {instanceId}. Was it a structure? {isStructure}. Trying to add to a cluster now...");
                buildable = BuildableCollection.GetBuildable(instanceId, isStructure);
            }
            else
            {
                buildable = cluster.Buildables.First(k => k.InstanceId == instanceId);
                cluster.Buildables.Remove(buildable);
            }

            if (buildable == null)
            {
                Logging.Write(this, $"Failed to find buildable with instance ID {instanceId}. Was it a structure? {isStructure}.");
                return;
            }

            var bestCluster = ClusteringTool.FindBestCluster(Clusters, buildable);

            if (bestCluster != null)
            {
                bestCluster.Buildables.Add(buildable);
                return;
            }

            var globalCluster = Clusters.FirstOrDefault(k => k.IsGlobalCluster);

            if (globalCluster != null)
                globalCluster.Buildables.Add(buildable);
            else
                Clusters.Add(new BaseCluster(this, true, new List<Buildable> {buildable}));
        }

        private void BuildableSpawned([NotNull] Buildable buildable)
        {
            var bestClusters = ClusteringTool.FindBestClusters(Clusters, buildable);

            var clusterCount = bestClusters.Count();
            if (clusterCount == 0)
            {
                var globalCluster = Clusters.FirstOrDefault(k => k.IsGlobalCluster);
                if (globalCluster != null)
                    globalCluster.Buildables.Add(buildable);
                else
                    Clusters.Add(new BaseCluster(this, true, new List<Buildable> {buildable}));

                return;
            }

            if (clusterCount > 1)
            {
                var allBuilds = bestClusters.SelectMany(k => k.Buildables).ToList();

                var newClusters = ClusteringTool.ClusterElements(allBuilds);

                foreach (var cluster in bestClusters)
                    UntrackCluster(cluster);

                foreach (var cluster in newClusters)
                    Clusters.Add(cluster);

                return;
            }

            bestClusters.First().Buildables.Add(buildable);
        }

        // Removes any tracking on said cluster.
        internal void UntrackCluster([NotNull] BaseCluster cluster)
        {
            Clusters.Remove(cluster);
        }

        private void ClustersChanged(object sender, [NotNull] NotifyCollectionChangedEventArgs e)
        {
            lock (m_InstanceCountLock)
            {
                lock (m_FreeInstanceIds)
                {
                    Logging.Verbose(this, $"Clusters were modified. Action: {e.Action}");
                    switch (e.Action)
                    {
                        case NotifyCollectionChangedAction.Add:
                            // Raise an event for every single item added.
                            foreach (var cluster in e.NewItems.Cast<BaseCluster>())
                            {
                                Logging.Verbose(this,
                                    $"New cluster created at: {cluster.AverageCenterPosition}\nMost common group: {cluster.CommonGroup}\nMost common owner: {cluster.CommonOwner}\nAll buildables: {string.Join(", ", cluster.Buildables.Select(k => k.Position))}");
                                OnClusterAdded?.Invoke(cluster);

                                if (m_InstanceCount == cluster.InstanceId)
                                    m_InstanceCount++;
                            }

                            break;
                        case NotifyCollectionChangedAction.Move:
                            // No event needed, it was just switched in the position of the list.
                            Logging.Verbose(this,
                                $"Cluster moved from index {e.OldStartingIndex} to index {e.NewStartingIndex}");
                            break;
                        case NotifyCollectionChangedAction.Remove:
                        case NotifyCollectionChangedAction.Replace:
                            // Raise an event for every single item removed.
                            Logging.Verbose(this, $"Cluster {e.Action.ToString().ToLower()}d at index {e.OldStartingIndex}");
                            foreach (var cluster in e.OldItems.Cast<BaseCluster>())
                            {
                                OnClusterRemoved?.Invoke(cluster);
                                m_FreeInstanceIds.Enqueue(cluster.InstanceId);
                            }

                            break;
                        case NotifyCollectionChangedAction.Reset:
                            Logging.Verbose(this, "Clusters collection was reset. Most likely a Clear() call.");
                            OnClustersCleared?.Invoke();
                            m_FreeInstanceIds.Clear();
                            m_InstanceCount = 0;
                            break;
                    }
                }
            }
        }
    }
}