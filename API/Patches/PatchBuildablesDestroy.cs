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
                ThreadUtil.assertIsGameThread();
                OnBuildableDestroyed?.Invoke(BuildableDirectory.GetBuildable(region.drops[index].model));
            }

            [HarmonyPatch(typeof(StructureManager), "destroyStructure")]
            [HarmonyPrefix]
            [UsedImplicitly]
            internal static void DestroyStructure([NotNull] StructureRegion region, ushort index)
            {
                ThreadUtil.assertIsGameThread();
                OnBuildableDestroyed?.Invoke(BuildableDirectory.GetBuildable(region.drops[index].model));
            }
        }
    }
}