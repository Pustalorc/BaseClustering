using Rocket.API;

namespace Pustalorc.Plugins.BaseClustering.Config
{
    public sealed class BaseClusteringPluginConfiguration : IRocketPluginConfiguration
    {
        public bool VerboseLogging;
        public float InitialRadius;
        public float MaxRadius;
        public byte MaxRadiusRechecks;

        public void LoadDefaults()
        {
            VerboseLogging = false;
            InitialRadius = 25f;
            MaxRadius = 75f;
            MaxRadiusRechecks = 50;
        }
    }
}