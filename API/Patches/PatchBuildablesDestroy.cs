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
    public static class PatchBuildablesDestroy
    {
        public static event BuildableChange OnBuildableDestroyed;

        [HarmonyPatch]
        internal static class InternalPatches
        {
            [HarmonyPatch(typeof(BarricadeManager), "destroyBarricade")]
            [HarmonyPrefix]
            [UsedImplicitly]
            internal static void DestroyBarricade([NotNull] BarricadeRegion region, ushort index)
            {
                var stopwatch = Stopwatch.StartNew();
                ThreadUtil.assertIsGameThread();
                OnBuildableDestroyed?.Invoke(BuildableCollection.GetBuildable(region.drops[index].model));

                stopwatch.Stop();
                var elapsed = stopwatch.ElapsedMilliseconds;
                if (elapsed > 10)
                    Logging.Write("DestroyBarricade",
                        $"Warning! Event hooks took too long to finish! Total time spent on hooks: {elapsed}ms",
                        ConsoleColor.Yellow);
            }

            [HarmonyPatch(typeof(StructureManager), "destroyStructure")]
            [HarmonyPrefix]
            [UsedImplicitly]
            internal static void DestroyStructure([NotNull] StructureRegion region, ushort index)
            {
                var stopwatch = Stopwatch.StartNew();
                ThreadUtil.assertIsGameThread();
                OnBuildableDestroyed?.Invoke(BuildableCollection.GetBuildable(region.drops[index].model));

                stopwatch.Stop();
                var elapsed = stopwatch.ElapsedMilliseconds;
                if (elapsed > 10)
                    Logging.Write("DestroyStructure",
                        $"Warning! Event hooks took too long to finish! Total time spent on hooks: {elapsed}ms",
                        ConsoleColor.Yellow);
            }
        }
    }
}