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
            [HarmonyPatch(typeof(BarricadeManager), "ReceiveTransformBarricade")]
            [HarmonyPostfix]
            [UsedImplicitly]
            internal static void ReceiveTransformBarricade(uint instanceID)
            {
                var timer = Stopwatch.StartNew();
                OnBuildableTransformed?.Invoke(BuildableCollection.GetBuildable(instanceID, false));

                timer.Stop();
                var elapsed = timer.ElapsedMilliseconds;
                if (elapsed > 10)
                    Logging.Write("ReceiveTransformBarricade",
                        $"Warning! Event hooks took too long to finish! Total time spent on hooks: {timer.ElapsedMilliseconds}ms",
                        ConsoleColor.Yellow);
            }

            [HarmonyPatch(typeof(StructureManager), "ReceiveTransformStructure")]
            [HarmonyPostfix]
            [UsedImplicitly]
            internal static void ReceiveTransformStructure(uint instanceID)
            {
                var timer = Stopwatch.StartNew();
                OnBuildableTransformed?.Invoke(BuildableCollection.GetBuildable(instanceID, true));

                timer.Stop();
                var elapsed = timer.ElapsedMilliseconds;
                if (elapsed > 10)
                    Logging.Write("ReceiveTransformStructure",
                        $"Warning! Event hooks took too long to finish! Total time spent on hooks: {timer.ElapsedMilliseconds}ms",
                        ConsoleColor.Yellow);
            }
        }
    }
}