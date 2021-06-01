using Pustalorc.Plugins.BaseClustering.API.BaseClusters;
using Rocket.API;

namespace Pustalorc.Plugins.BaseClustering.Config
{
    /// <summary>
    /// Configuration for the plugin when it comes to how it should operate and handle things.
    /// </summary>
    public sealed class BaseClusteringPluginConfiguration : IRocketPluginConfiguration
    {
        /// <summary>
        /// If verbose level logging (i.e: extra logging) should be enabled or not.
        /// </summary>
        public bool VerboseLogging;

        /// <summary>
        /// If the <see cref="BaseClusterDirectory"/> should be instantiated and loaded or not.
        /// </summary>
        public bool EnableClustering;

        /// <summary>
        /// The maximum distance between 2 structure objects for them to be considered part of a <see cref="BaseCluster"/>.
        /// <br/>
        /// Distance is in meters by unity's measurements. Squaring is needed if using <see cref="UnityEngine.Vector3.sqrMagnitude"/> for speed.
        /// </summary>
        public float MaxDistanceBetweenStructures;

        /// <summary>
        /// The maximum distance between a structure object and a position for it to be considered part of, or inside of a <see cref="BaseCluster"/>.
        /// <br/>
        /// Distance is in meters by unity's measurements. Squaring is needed if using <see cref="UnityEngine.Vector3.sqrMagnitude"/> for speed.
        /// </summary>
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