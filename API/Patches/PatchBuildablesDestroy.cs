using HarmonyLib;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Delegates;
using SDG.Unturned;

namespace Pustalorc.Plugins.BaseClustering.API.Patches
{
    public static class PatchBuildablesDestroy
    {
        public static event BuildableDeleted? OnBuildableDestroyed;

        [HarmonyPatch]
        internal static class InternalPatches
        {
            [HarmonyPatch(typeof(BarricadeManager), "destroyBarricade")]
            [HarmonyPrefix]
            [UsedImplicitly]
            internal static void DestroyBarricade(BarricadeRegion region, ushort index)
            {
                ThreadUtil.assertIsGameThread();
                OnBuildableDestroyed?.Invoke(region.drops[index].model);
            }

            [HarmonyPatch(typeof(StructureManager), "destroyStructure")]
            [HarmonyPrefix]
            [UsedImplicitly]
            internal static void DestroyStructure(StructureRegion region, ushort index)
            {
                ThreadUtil.assertIsGameThread();
                OnBuildableDestroyed?.Invoke(region.drops[index].model);
            }
        }
    }
}