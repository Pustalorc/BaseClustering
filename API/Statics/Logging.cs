using System;
using JetBrains.Annotations;
using Rocket.API;
using Rocket.Core.Logging;

namespace Pustalorc.Plugins.BaseClustering.API.Statics
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

        public static void Write(object source, object message, ConsoleColor consoleColor = ConsoleColor.Green,
            bool logInRocket = true, [CanBeNull] object rocketMessage = null, ConsoleColor? rocketColor = null)
        {
            Console.ForegroundColor = consoleColor;
            Console.WriteLine($"[{source}]: {message}");

            if (logInRocket)
                Logger.ExternalLog(rocketMessage ?? message, rocketColor ?? consoleColor);

            Console.ResetColor();
        }

        public static void PluginLoaded([NotNull] IRocketPlugin plugin)
        {
            Write(plugin.Name, $"{plugin.Name}, by Pustalorc, has been loaded.");
        }

        public static void PluginUnloaded([NotNull] IRocketPlugin plugin)
        {
            Write(plugin.Name, $"{plugin.Name}, by Pustalorc, has been unloaded.");
        }
    }
}