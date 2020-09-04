using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Statics;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.API.Classes
{
    public class BaseCluster
    {
        private ObservableCollection<Buildable> m_Buildables;

        private static float MaxExpandRadius => BaseClusteringPlugin.Instance != null
            ? BaseClusteringPlugin.Instance.Configuration.Instance.MaxClusterSelfExpandRadius
            : 75f;

        public ulong TotalHealth => m_Buildables.SumUlong(k => k.Health);

        public ulong CommonOwner => m_Buildables.GroupBy(k => k.Owner)
            .OrderByDescending(k => k.Count())
            .Select(g => g.Key).ToList().FirstOrDefault();

        public ulong CommonGroup => m_Buildables.GroupBy(k => k.Group)
            .OrderByDescending(k => k.Count())
            .Select(g => g.Key).ToList().FirstOrDefault();

        public Vector3 AverageCenterPosition => m_Buildables.AverageCenter(k => k.Position);

        public bool IsGlobalCluster;

        public byte AngleX;
        public byte AngleY;
        public byte AngleZ;

        public Vector3 CenterBuildable;
        public double Radius;

        public ObservableCollection<Buildable> Buildables
        {
            get => m_Buildables;
            private set
            {
                if (m_Buildables != null)
                    m_Buildables.CollectionChanged -= BuildablesChanged;

                m_Buildables = value;

                m_Buildables.CollectionChanged -= BuildablesChanged;
                m_Buildables.CollectionChanged += BuildablesChanged;
            }
        }

        public BaseCluster()
        {
            Buildables = new ObservableCollection<Buildable>();
            CenterBuildable = Vector3.zero;
            Radius = 0;
            AngleX = AngleY = AngleZ = 0;
        }

        public BaseCluster([NotNull] List<Buildable> buildables, Vector3 center, double radius, bool isGlobalCluster)
        {
            Buildables = new ObservableCollection<Buildable>(buildables);
            CenterBuildable = center;
            Radius = radius;

            double angleX = 0, angleY = 0, angleZ = 0;

            foreach (var build in Buildables)
            {
                angleX += build.AngleX;
                angleY += build.AngleY;
                angleZ += build.AngleZ;
            }

            AngleX = (byte) (angleX / Buildables.Count);
            AngleY = (byte) (angleY / Buildables.Count);
            AngleZ = (byte) (angleZ / Buildables.Count);

            IsGlobalCluster = isGlobalCluster;
        }

        private void BuildablesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (Buildables.Count == 0)
            {
                BaseClusteringPlugin.Instance.DestroyCluster(this);
                return;
            }

            decimal angleX = 0, angleY = 0, angleZ = 0;

            foreach (var build in Buildables)
            {
                angleX += build.AngleX;
                angleY += build.AngleY;
                angleZ += build.AngleZ;
            }

            AngleX = (byte) (angleX / Buildables.Count);
            AngleY = (byte) (angleY / Buildables.Count);
            AngleZ = (byte) (angleZ / Buildables.Count);

            var centerIndex = Buildables.GetCenterIndex();
            CenterBuildable = Buildables[centerIndex].Position;

            var radiiDist = Buildables.GetDistances(k => k.Position, CenterBuildable).ToList();

            if (!(radiiDist.Max() > 0)) return;

            var newRadius = radiiDist.Max();

            if (newRadius > MaxExpandRadius)
                newRadius = MaxExpandRadius;

            Radius = newRadius;
        }
    }
}