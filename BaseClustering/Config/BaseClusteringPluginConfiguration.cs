using System;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Configuration.Interfaces;
using Pustalorc.Libraries.Logging.API.Loggers.Configuration.Interfaces;
using Pustalorc.Libraries.Logging.API.LogLevels.Implementations;
using Rocket.API;

namespace Pustalorc.Plugins.BaseClustering.Config;

/// <inheritdoc cref="Pustalorc.Libraries.BaseClustering.API.BaseClusters.Configuration.Interfaces.IClusterRules" />
/// <summary>
///     Configuration for the plugin when it comes to how it should operate and handle things.
/// </summary>
[Serializable]
public sealed class BaseClusteringPluginConfiguration : IRocketPluginConfiguration, IClusterRules, ILoggerConfiguration
{
    /// <inheritdoc />
    public byte MaxLogLevel { get; set; }

    /// <inheritdoc />
    public float MaxDistanceBetweenStructures { get; set; }

    /// <inheritdoc />
    public float MaxDistanceToConsiderPartOfBase { get; set; }

    /// <summary>
    ///     Loads the default values for the config.
    /// </summary>
    public void LoadDefaults()
    {
        MaxLogLevel = LogLevel.Info.Level;
        MaxDistanceBetweenStructures = 6.1f;
        MaxDistanceToConsiderPartOfBase = 10f;
    }
}