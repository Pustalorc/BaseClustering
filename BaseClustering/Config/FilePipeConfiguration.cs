using System;
using Pustalorc.Libraries.Logging.API.Pipes.Configuration;
using Pustalorc.Libraries.Logging.LogLevels;
using Pustalorc.Libraries.Logging.Pipes.Configuration;

namespace Pustalorc.Plugins.BaseClustering.Config;

/// <inheritdoc />
[Serializable]
public class FilePipeConfiguration : IFilePipeConfiguration
{
    /// <inheritdoc />
    public byte MaxLogLevel { get; set; } = LogLevel.Debug.Level;

    /// <inheritdoc />
    public string PipeName => Default.PipeName;

    /// <inheritdoc />
    public string MessageFormat => Default.MessageFormat;

    /// <inheritdoc />
    public byte MinLogLevel => Default.MinLogLevel;

    /// <inheritdoc />
    public string FileNameFormat => Default.FileNameFormat;

    private DefaultFilePipeConfiguration Default { get; } = new();
}