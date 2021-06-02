using System;
using Pustalorc.Plugins.BaseClustering.Config;
using Rocket.Core.Logging;
using Rocket.Core.Plugins;

namespace Pustalorc.Plugins.BaseClustering.API.Utilities
{
    /// <summary>
    /// Custom class for logging. To be used only by this plugin.
    /// </summary>
    internal static class Logging
    {
        /// <summary>
        /// Logs a Verbose level message, if <see cref="BaseClusteringPluginConfiguration.VerboseLogging"/> is true.
        /// </summary>
        /// <param name="source">The apparent source that sent this message.</param>
        /// <param name="message">The actual message to be printed out.</param>
        /// <param name="consoleColor">The <see cref="ConsoleColor"/> to use in console.</param>
        /// <param name="logInRocket">If the verbose message should be logged in rocketmod's log file.</param>
        /// <param name="rocketMessage">If the message is to be modified when logging to rocketmod's log file, this value will be used if it is not null.</param>
        /// <param name="rocketColor">If the color that rocket perceives was used is to be modified when logging to rocketmod's log file, this value will be used if it is not null.</param>
        public static void Verbose(object source, object message, ConsoleColor consoleColor = ConsoleColor.Green,
            bool logInRocket = true, object? rocketMessage = null, ConsoleColor? rocketColor = null)
        {
            if (BaseClusteringPlugin.Instance != null &&
                (!BaseClusteringPlugin.Instance.Configuration?.Instance?.VerboseLogging ?? false)) return;

            Console.ForegroundColor = consoleColor;
            Console.WriteLine($"[VERBOSE {source}]: {message}");

            if (logInRocket)
                Logger.ExternalLog($"[VERBOSE {source}]: " + (rocketMessage ?? message), rocketColor ?? consoleColor);

            Console.ResetColor();
        }

        /// <summary>
        /// Logs a normal level message.
        /// </summary>
        /// <param name="source">
        /// The apparent source that sent this message.
        /// <br/>
        /// If it is of type <see cref="RocketPlugin"/> then the version will be retrieved from the file to log alongside its name.</param>
        /// <param name="message">The actual message to be printed out.</param>
        /// <param name="consoleColor">The <see cref="ConsoleColor"/> to use in console.</param>
        /// <param name="logInRocket">If the verbose message should be logged in rocketmod's log file.</param>
        /// <param name="rocketMessage">If the message is to be modified when logging to rocketmod's log file, this value will be used if it is not null.</param>
        /// <param name="rocketColor">If the color that rocket perceives was used is to be modified when logging to rocketmod's log file, this value will be used if it is not null.</param>
        public static void Write(object source, object message, ConsoleColor consoleColor = ConsoleColor.Green,
            bool logInRocket = true, object? rocketMessage = null, ConsoleColor? rocketColor = null)
        {
            var sourceIdent = source.ToString();

            if (source is RocketPlugin pl)
            {
                var pluginVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo(pl.Assembly.Location)
                    .ProductVersion;
                sourceIdent = $"{pl.Name} v{pluginVersion}";
            }

            Console.ForegroundColor = consoleColor;
            Console.WriteLine($"[{sourceIdent}]: {message}");

            if (logInRocket)
                Logger.ExternalLog(rocketMessage ?? message, rocketColor ?? consoleColor);

            Console.ResetColor();
        }

        /// <summary>
        /// Logs that a plugin was loaded.
        /// </summary>
        /// <param name="plugin">The instance of the plugin that was loaded.</param>
        public static void PluginLoaded(RocketPlugin plugin)
        {
            var pluginVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo(plugin.Assembly.Location)
                .ProductVersion;
            var pluginIdentity = $"{plugin.Name} v{pluginVersion}";
            Write(pluginIdentity, $"{pluginIdentity}, by Pustalorc, has been loaded.");
        }

        /// <summary>
        /// Logs that a plugin was unloaded.
        /// </summary>
        /// <param name="plugin">The instance of the plugin that was unloaded.</param>
        public static void PluginUnloaded(RocketPlugin plugin)
        {
            var pluginVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo(plugin.Assembly.Location)
                .ProductVersion;
            var pluginIdentity = $"{plugin.Name} v{pluginVersion}";
            Write(pluginIdentity, $"{pluginIdentity}, by Pustalorc, has been unloaded.");
        }
    }
}