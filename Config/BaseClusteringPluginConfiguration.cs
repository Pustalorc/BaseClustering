using Rocket.API;

namespace Pustalorc.Plugins.BaseClustering.Config
{
    public sealed class BaseClusteringPluginConfiguration : IRocketPluginConfiguration
    {
        public bool VerboseLogging;
        public float MaxBaseDistanceCheck;

        public void LoadDefaults()
        {
            VerboseLogging = false;
            MaxBaseDistanceCheck = 10f;
        }
    }
}