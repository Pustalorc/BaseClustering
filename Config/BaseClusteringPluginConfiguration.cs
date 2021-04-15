using Rocket.API;

namespace Pustalorc.Plugins.BaseClustering.Config
{
    public sealed class BaseClusteringPluginConfiguration : IRocketPluginConfiguration
    {
        public bool VerboseLogging;
        public bool EnableClustering;
        public float MaxDistanceBetweenStructures;
        public float MaxDistanceToConsiderPartOfBase;

        public void LoadDefaults()
        {
            VerboseLogging = false;
            EnableClustering = true;
            MaxDistanceBetweenStructures = 6.1f;
            MaxDistanceToConsiderPartOfBase = 10f;
        }
    }
}