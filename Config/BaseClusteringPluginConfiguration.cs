using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.BaseClusters;
using Pustalorc.Plugins.BaseClustering.API.Buildables;
using Rocket.API;

namespace Pustalorc.Plugins.BaseClustering.Config
{
    /// <summary>
    /// Configuration for the plugin when it comes to how it should operate and handle things.
    /// </summary>
    [UsedImplicitly]
    public sealed class BaseClusteringPluginConfiguration : IRocketPluginConfiguration
    {
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

        /// <summary>
        /// The default buildable capacity when initializing <see cref="BuildableDirectory"/>.
        /// </summary>
        public int BuildableCapacity;

        /// <summary>
        /// Loads the default values for the config.
        /// </summary>
        public void LoadDefaults()
        {
            EnableClustering = true;
            MaxDistanceBetweenStructures = 6.1f;
            MaxDistanceToConsiderPartOfBase = 10f;
            BuildableCapacity = 60000;
        }
    }
}