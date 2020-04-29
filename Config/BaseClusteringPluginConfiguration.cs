using Rocket.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pustalorc.Plugins.BaseClustering.Config
{
    public sealed class BaseClusteringPluginConfiguration : IRocketPluginConfiguration
    {
        public float InitialRadius;
        public float MaxRadius;
        public byte MaxRadiusRechecks;

        public void LoadDefaults()
        {
            InitialRadius = 25f;
            MaxRadius = 75f;
            MaxRadiusRechecks = 50;
        }
    }
}
