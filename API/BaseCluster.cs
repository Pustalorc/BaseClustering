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
        private ObservableCollection<Buildable> m_Buildables;

        public ulong InstanceId { get; }

        public ulong CommonOwner => m_Buildables.GroupBy(k => k.Owner)
            .OrderByDescending(k => k.Count())
            .Select(g => g.Key).ToList().FirstOrDefault();

        public ulong CommonGroup => m_Buildables.GroupBy(k => k.Group)
            .OrderByDescending(k => k.Count())
            .Select(g => g.Key).ToList().FirstOrDefault();

        public Vector3 AverageCenterPosition => m_Buildables.AverageCenter(k => k.Position);

        public bool IsGlobalCluster;
        public double Radius;

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

        public BaseCluster([NotNull] List<Buildable> buildables, double radius, bool isGlobalCluster, ulong instanceId)
        {
            Buildables = new ObservableCollection<Buildable>(buildables);
            Radius = radius;

            IsGlobalCluster = isGlobalCluster;
            InstanceId = instanceId;

            Logging.Verbose("New cluster",
                $"A new cluster (ID: {InstanceId}) was created at {AverageCenterPosition}. Radius of {radius}. Total buildables: {Buildables.Count}. Global cluster {IsGlobalCluster}");
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

        private void BuildablesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (m_Buildables.Count == 0)
            {
                BaseClusteringPlugin.Instance.UntrackCluster(this);
                return;
            }

            var radiiDist = Buildables.GetDistances(k => k.Position, AverageCenterPosition).ToList();
            Radius = radiiDist.Max() + BaseClusteringPlugin.Instance.Configuration.Instance.ExtraRadius;
        }
    }
}