using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Buildables;
using Pustalorc.Plugins.BaseClustering.API.Utils;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.API
{
    public class BaseCluster
    {
        private readonly BaseClusteringPlugin m_PluginInstance;
        private ObservableCollection<Buildable> m_Buildables;

        public ulong CommonOwner => m_Buildables.GroupBy(k => k.Owner)
            .OrderByDescending(k => k.Count())
            .Select(g => g.Key).ToList().FirstOrDefault();

        public ulong CommonGroup => m_Buildables.GroupBy(k => k.Group)
            .OrderByDescending(k => k.Count())
            .Select(g => g.Key).ToList().FirstOrDefault();

        public Vector3 AverageCenterPosition => m_Buildables.AverageCenter(k => k.Position);

        public int InstanceId { get; }

        public bool IsGlobalCluster { get; }

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
            IsGlobalCluster = isGlobalCluster;
            InstanceId = instanceId;
            Buildables = new ObservableCollection<Buildable>(buildables);
            Logging.Verbose("New cluster",
                $"A new cluster (ID: {InstanceId}) was created at {AverageCenterPosition}. Total buildables: {Buildables.Count}.");
        }

        public BaseCluster([NotNull] BaseClusteringPlugin pluginInstance, bool isGlobalCluster,
            [NotNull] List<Buildable> buildables)
        {
            m_PluginInstance = pluginInstance;
            IsGlobalCluster = isGlobalCluster;
            InstanceId = pluginInstance.GetBestInstanceId();
            Buildables = new ObservableCollection<Buildable>(buildables);
            Logging.Verbose("New cluster",
                $"A new cluster (ID: {InstanceId}) was created at {AverageCenterPosition}. Total buildables: {Buildables.Count}.");
        }

        public void Destroy()
        {
            foreach (var buildable in Buildables)
            {
                buildable.SafeDestroy();
                m_Buildables.Remove(buildable);
            }

            BaseClusteringPlugin.Instance.UntrackCluster(this);
        }

        public bool IsWithinRange(Vector3 position)
        {
            var distanceCheck = m_PluginInstance.Configuration.Instance.MaxDistanceToConsiderPartOfBase;

            return Buildables.Any(k => (k.Position - position).sqrMagnitude <= distanceCheck);
        }

        private void BuildablesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (m_Buildables.Count != 0)
                return;

            // Global clusters should not auto-delete, as there should only be one global cluster.
            if (IsGlobalCluster)
                return;

            BaseClusteringPlugin.Instance.UntrackCluster(this);
        }
    }
}