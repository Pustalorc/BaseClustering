// ReSharper disable InconsistentNaming

using System;
using System.Diagnostics;
using HarmonyLib;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Buildables;
using Pustalorc.Plugins.BaseClustering.API.Delegates;
using Pustalorc.Plugins.BaseClustering.API.Utilities;
using SDG.Unturned;

namespace Pustalorc.Plugins.BaseClustering.API.Patches
{
    public static class PatchBuildableTransforms
    {
        public static event BuildableChange OnBuildableTransformed;

        [HarmonyPatch]
        internal static class InternalPatches
        {
            [HarmonyPatch(typeof(BarricadeManager), "askTransformBarricade")]
            [HarmonyPostfix]
            [UsedImplicitly]
            internal static void AskTransformBarricade(uint instanceID)
            {
                var timer = Stopwatch.StartNew();
                OnBuildableTransformed?.Invoke(BuildableCollection.GetBuildable(instanceID, false));

                timer.Stop();
                var elapsed = timer.ElapsedMilliseconds;
                if (elapsed > 10)
                    Logging.Write("AskTransformBarricade",
                        $"Warning! Event hooks took too long to finish! Total time spent on hooks: {timer.ElapsedMilliseconds}ms",
                        ConsoleColor.Yellow);
            }

            [HarmonyPatch(typeof(StructureManager), "askTransformStructure")]
            [HarmonyPostfix]
            [UsedImplicitly]
            internal static void AskTransformStructure(uint instanceID)
            {
                var timer = Stopwatch.StartNew();
                OnBuildableTransformed?.Invoke(BuildableCollection.GetBuildable(instanceID, true));

                timer.Stop();
                var elapsed = timer.ElapsedMilliseconds;
                if (elapsed > 10)
                    Logging.Write("AskTransformStructure",
                        $"Warning! Event hooks took too long to finish! Total time spent on hooks: {timer.ElapsedMilliseconds}ms",
                        ConsoleColor.Yellow);
            }
        }
    }
}