using System.Collections.Generic;
using Rocket.API;

namespace Pustalorc.Plugins.BaseClustering.Config
{
    public sealed class BaseClusteringPluginConfiguration : IRocketPluginConfiguration
    {
        public bool VerboseLogging;
        public float MaxClusterSelfExpandRadius;
        public EClusteringStyle ClusteringStyle;
        public BruteforceOptions BruteforceOptions;
        public RustOptions RustOptions;

        public void LoadDefaults()
        {
            VerboseLogging = false;
            MaxClusterSelfExpandRadius = 75f;
            ClusteringStyle = EClusteringStyle.Hybrid;
            BruteforceOptions = new BruteforceOptions(25f, 75f, 10);
            RustOptions = new RustOptions(new List<ushort> {31, 51, 52, 369, 1262, 1263, 1264}, 1.73205078f);
        }
    }
}