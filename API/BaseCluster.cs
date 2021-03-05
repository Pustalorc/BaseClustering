using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Buildables;
using Pustalorc.Plugins.BaseClustering.API.Delegates;
using Pustalorc.Plugins.BaseClustering.API.Utilities;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.API
{
    public class BaseCluster
    {
        private readonly BaseClusteringPlugin m_PluginInstance;
        private readonly BuildableDamageQueue m_DamageQueue;
        private ObservableCollection<Buildable> m_Buildables;

        public event BuildableChange OnBuildableAdded;
        public event BuildableChange OnBuildableRemoved;

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

        public ObservableCollection<Buildable> Buildables
        {
            get => m_Buildables;
            private set
            {
                if (m_Buildables == null)
                {
                    m_Buildables = value;
                    m_Buildables.CollectionChanged += BuildablesChanged;
                    return;
                }

                m_Buildables.Clear();
                foreach (var build in value)
                    m_Buildables.Add(build);
            }
        }

        public BaseCluster([NotNull] BaseClusteringPlugin pluginInstance, int instanceId, bool isGlobalCluster,
            [NotNull] List<Buildable> buildables)
        {
            m_PluginInstance = pluginInstance;
            m_DamageQueue = new BuildableDamageQueue(pluginInstance);
            IsGlobalCluster = isGlobalCluster;
            InstanceId = instanceId;
            Buildables = new ObservableCollection<Buildable>(buildables);
        }

        public BaseCluster([NotNull] BaseClusteringPlugin pluginInstance, bool isGlobalCluster,
            [NotNull] List<Buildable> buildables)
        {
            m_PluginInstance = pluginInstance;
            m_DamageQueue = new BuildableDamageQueue(pluginInstance);
            IsGlobalCluster = isGlobalCluster;
            InstanceId = pluginInstance.GetBestInstanceId();
            Buildables = new ObservableCollection<Buildable>(buildables);
        }

        public void Destroy(bool shouldDropItems = true)
        {
            IsBeingDestroyed = true;

            foreach (var buildable in Buildables)
                m_DamageQueue.Enqueue(new QueuedDamage(buildable, ushort.MaxValue, false, !shouldDropItems));

            m_PluginInstance.Clusters.Remove(this);
        }

        public bool IsWithinRange(Buildable buildable)
        {
            var structures = Buildables.OfType<StructureBuildable>();
            var distanceCheck = buildable is StructureBuildable
                ? Mathf.Pow(m_PluginInstance.Configuration.Instance.MaxDistanceBetweenStructures, 2)
                : Mathf.Pow(m_PluginInstance.Configuration.Instance.MaxDistanceToConsiderPartOfBase, 2);

            return structures.Any(k => (k.Position - buildable.Position).sqrMagnitude <= distanceCheck);
        }

        public bool IsWithinRange(Vector3 position)
        {
            var distanceCheck = Mathf.Pow(m_PluginInstance.Configuration.Instance.MaxDistanceToConsiderPartOfBase, 2);

            return Buildables.Any(k => (k.Position - position).sqrMagnitude <= distanceCheck);
        }

        private bool VerifyStructureIntegrityForced([NotNull] IList<StructureBuildable> structures)
        {
            var maxStructureDistance =
                Mathf.Pow(m_PluginInstance.Configuration.Instance.MaxDistanceBetweenStructures, 2);
            var succeeded = new List<StructureBuildable>();
            var result = true;

            var random = structures[Random.Range(0, structures.Count)];
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

        public bool VerifyStructureIntegrity([CanBeNull] Buildable removedBuildable = null)
        {
            var allStructures = Buildables.OfType<StructureBuildable>().ToList();

            if (allStructures.Count <= 0)
                return false;

            var affectedArea = new List<StructureBuildable>();

            var result = true;

            if (removedBuildable != null)
            {
                var maxDist = Mathf.Pow(m_PluginInstance.Configuration.Instance.DestroyIntegrityCheckDistance, 2);
                affectedArea = allStructures
                    .Where(k => (removedBuildable.Position - k.Position).sqrMagnitude <= maxDist).ToList();
                result = VerifyStructureIntegrityForced(affectedArea);
            }

            if (!result && affectedArea.Count != allStructures.Count)
                result = VerifyStructureIntegrityForced(allStructures);

            return result;
        }

        public bool VerifyBarricadeIntegrity()
        {
            var barricades = Buildables.OfType<BarricadeBuildable>().ToList();
            var structures = Buildables.OfType<StructureBuildable>().ToList();
            var maxBuildableDistance =
                Mathf.Pow(m_PluginInstance.Configuration.Instance.MaxDistanceToConsiderPartOfBase, 2);

            return barricades.All(br =>
                structures.Exists(k => (br.Position - k.Position).sqrMagnitude <= maxBuildableDistance));
        }

        public void VerifyAndCorrectIntegrity([CanBeNull] Buildable removedBuildable = null)
        {
            lock (m_PluginInstance.Clusters)
            {
                var structureIntegrity = VerifyStructureIntegrity(removedBuildable);
                var barricadeIntegrity = VerifyBarricadeIntegrity();

                // If the base is still integrally sound, skip the rest of the code
                if (structureIntegrity && barricadeIntegrity) return;

                var globalCluster = m_PluginInstance.Clusters.FirstOrDefault(k => k.IsGlobalCluster);

                IsBeingDestroyed = true;
                // If the structure is still integral, check the barricades and fix any non-integral parts.
                if (structureIntegrity)
                {
                    // Get all the barricades that are too far from the cluster in a copied list.
                    foreach (var b in Buildables.OfType<BarricadeBuildable>().Where(k => !IsWithinRange(k)).ToList())
                    {
                        // Find the next best cluster that this element is within
                        var bestCluster = m_PluginInstance.ClusteringTool.FindBestCluster(b);

                        // If something is found, check that its not the same cluster we are already in.
                        if (bestCluster != null)
                        {
                            if (bestCluster != this)
                            {
                                // If its a different cluster, remove it from the current cluster and add it to the new one.
                                Buildables.Remove(b);
                                bestCluster.Buildables.Add(b);
                            }

                            continue;
                        }

                        // If no best cluster is found, check if we have a global cluster. If we do, add the barricade to it. If we don't, create a new global cluster.
                        if (globalCluster != null)
                            globalCluster.Buildables.Add(b);
                        else
                            m_PluginInstance.Clusters.Add(new BaseCluster(m_PluginInstance, true,
                                new List<Buildable> {b}));

                        Buildables.Remove(b);
                    }

                    IsBeingDestroyed = false;
                    return;
                }

                // First, ask the clustering tool to generate a new set of clusters from the current buildables.
                var clusterRegened = m_PluginInstance.ClusteringTool.ClusterElements(Buildables.ToList());
                // Now filter the results to include those which are not global clusters
                var clustersNoGlobal = clusterRegened.Where(k => !k.IsGlobalCluster);
                // Filter the results to include those which are global clusters
                var clustersGlobal = clusterRegened.Where(k => k.IsGlobalCluster);

                // Select all the clusters, except for the largest one.
                foreach (var c in clustersNoGlobal.OrderByDescending(k => k.Buildables.Count).Skip(1).ToList())
                {
                    // Remove any of the elements on the new cluster from the old one.
                    foreach (var b in c.Buildables)
                        Buildables.Remove(b);

                    // Add the new cluster to the collection.
                    m_PluginInstance.Clusters.Add(c);
                }

                // For any global clusters pick each of the buildables if we add it to a new global cluster or to the existing one.
                foreach (var barricade in clustersGlobal.SelectMany(k => k.Buildables))
                {
                    // Find the next best cluster that this element is within
                    var bestCluster = m_PluginInstance.ClusteringTool.FindBestCluster(barricade);

                    // If something is found, check that its not the same cluster we are already in.
                    if (bestCluster != null)
                    {
                        if (bestCluster != this)
                        {
                            // If its a different cluster, remove it from the current cluster and add it to the new one.
                            Buildables.Remove(barricade);
                            bestCluster.Buildables.Add(barricade);
                        }

                        continue;
                    }

                    // If no best cluster is found, check if we have a global cluster. If we do, add the barricade to it. If we don't, create a new global cluster.
                    if (globalCluster != null)
                        globalCluster.Buildables.Add(barricade);
                    else
                        m_PluginInstance.Clusters.Add(new BaseCluster(m_PluginInstance, true,
                            new List<Buildable> {barricade}));

                    // Remove from the current cluster the barricade, this way once the "cluster removed" event is fired, other programs know if anything has remained.
                    Buildables.Remove(barricade);
                }

                // Finally, if there's no structure buildables left int his cluster, call to remove it.
                if (!Buildables.OfType<StructureBuildable>().Any())
                    m_PluginInstance.Clusters.Remove(this);

                IsBeingDestroyed = false;
            }
        }

        private void BuildablesChanged(object sender, [NotNull] NotifyCollectionChangedEventArgs e)
        {
            Buildable removedBuild = null;
            // Raise event for cluster changes.
            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (Buildable build in e.NewItems)
                        OnBuildableAdded?.Invoke(build);

                    break;
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Replace:
                    // Is it slow because of the mass multi-threading? Could be, but shouldn't, as all should be queued.
                    foreach (Buildable build in e.OldItems)
                    {
                        OnBuildableRemoved?.Invoke(build);
                        removedBuild = build;
                    }

                    break;
            }

            // Check if we should auto delete, change cluster, etc.
            // No checking should be done if we are destroying this cluster with Destroy() or if its a global cluster.
            if (IsBeingDestroyed || IsGlobalCluster)
                return;

            VerifyAndCorrectIntegrity(removedBuild);
        }
    }
}