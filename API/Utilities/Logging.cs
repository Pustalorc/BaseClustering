using System;
using JetBrains.Annotations;
using Rocket.Core.Logging;
using Rocket.Core.Plugins;

namespace Pustalorc.Plugins.BaseClustering.API.Utilities
{
    internal static class Logging
    {
        public static void Verbose(object source, object message, ConsoleColor consoleColor = ConsoleColor.Green,
            bool logInRocket = true, [CanBeNull] object rocketMessage = null, ConsoleColor? rocketColor = null)
        {
            if (BaseClusteringPlugin.Instance != null &&
                (!BaseClusteringPlugin.Instance.Configuration?.Instance?.VerboseLogging ?? false)) return;

            Console.ForegroundColor = consoleColor;
            Console.WriteLine($"[VERBOSE {source}]: {message}");

            if (logInRocket)
                Logger.ExternalLog($"[VERBOSE {source}]: " + (rocketMessage ?? message), rocketColor ?? consoleColor);

            Console.ResetColor();
        }

        public static void Write([NotNull] object source, object message, ConsoleColor consoleColor = ConsoleColor.Green,
            bool logInRocket = true, [CanBeNull] object rocketMessage = null, ConsoleColor? rocketColor = null)
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

        public static void PluginLoaded([NotNull] RocketPlugin plugin)
        {
            var pluginVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo(plugin.Assembly.Location)
                .ProductVersion;
            var pluginIdentity = $"{plugin.Name} v{pluginVersion}";
            Write(pluginIdentity, $"{pluginIdentity}, by Pustalorc, has been loaded.");
        }

        public static void PluginUnloaded([NotNull] RocketPlugin plugin)
        {
            var pluginVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo(plugin.Assembly.Location)
                .ProductVersion;
            var pluginIdentity = $"{plugin.Name} v{pluginVersion}";
            Write(pluginIdentity, $"{pluginIdentity}, by Pustalorc, has been unloaded.");
        }
    }
}