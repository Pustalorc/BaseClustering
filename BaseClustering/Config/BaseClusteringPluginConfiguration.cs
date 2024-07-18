using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Configuration.Interfaces;
using Pustalorc.Libraries.Logging.API.Loggers.Configuration;
using Pustalorc.Libraries.Logging.API.Pipes.Configuration;
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
    public float MaxDistanceBetweenStructures { get; set; }

    /// <inheritdoc />
    public float MaxDistanceToConsiderPartOfBase { get; set; }

    /// <inheritdoc />
    [XmlIgnore]
    public List<IPipeConfiguration> PipeSettings =>
    [
        ConsoleLogSettings,
        FileLogSettings
    ];

    /// <summary>
    ///     The log settings for the ConsolePipe
    /// </summary>
    public ConsolePipeConfiguration ConsoleLogSettings { get; set; } = new();


    /// <summary>
    ///     The log settings for the FilePipe
    /// </summary>
    public FilePipeConfiguration FileLogSettings { get; set; } = new();

    /// <summary>
    ///     Loads the default values for the config.
    /// </summary>
    public void LoadDefaults()
    {
        MaxDistanceBetweenStructures = 6.1f;
        MaxDistanceToConsiderPartOfBase = 10f;
        ConsoleLogSettings = new ConsolePipeConfiguration();
        FileLogSettings = new FilePipeConfiguration();
    }
}