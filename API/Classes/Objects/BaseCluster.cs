using Pustalorc.Plugins.BaseClustering.API.Statics;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.API.Classes.Objects
{
    public class BaseCluster
    {
        private ObservableCollection<Buildable> _buildables;

        public Vector3 Center;
        public double Radius;
        public byte AngleX;
        public byte AngleY;
        public byte AngleZ;

        public ObservableCollection<Buildable> Buildables
        {
            get => _buildables;
            private set
            {
                if (_buildables != null)
                    _buildables.CollectionChanged -= BuildablesChanged;

                _buildables = value;

                _buildables.CollectionChanged -= BuildablesChanged;
                _buildables.CollectionChanged += BuildablesChanged;
            }
        }

        public BaseCluster()
        {
            Buildables = new ObservableCollection<Buildable>();
            Center = Vector3.zero;
            Radius = 0;
            AngleX = AngleY = AngleZ = 0;
        }

        public BaseCluster(List<Buildable> buildables, Vector3 center, double radius)
        {
            Buildables = new ObservableCollection<Buildable>(buildables);
            Center = center;
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
        }

        private void BuildablesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
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

            var globalMean = Buildables.AverageCenter(k => k.Position);
            var globalScalar = Buildables.GetScalar(k => k.Position);
            var globalDensity = Buildables.GetDensity(k => k.Position, globalMean, globalScalar);
            var centerIndex = globalDensity.IndexOf(globalDensity.MinVector3());
            Center = Buildables[centerIndex].Position;

            var radiiDist = Buildables.GetDistances(k => k.Position, Center);
            if (radiiDist.Max() > 0)
            {
                var newRadius = radiiDist.Max();

                if (newRadius > BaseClusteringPlugin.Instance.Configuration.Instance.MaxRadius)
                    newRadius = BaseClusteringPlugin.Instance.Configuration.Instance.MaxRadius;

                Radius = newRadius;
            }
        }
    }
}