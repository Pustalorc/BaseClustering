using HarmonyLib;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Delegates;
using SDG.Unturned;

namespace Pustalorc.Plugins.BaseClustering.API.Patches
{
    /// <summary>
    /// A patch for barricades and structures being destroyed.
    /// </summary>
    public static class PatchBuildablesDestroy
    {
        /// <summary>
        /// Event is fired whenever a barricade or structure is destroyed.
        /// </summary>
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
                OnBuildableDestroyed?.Invoke(region.drops[index].instanceID, false);
            }

            [HarmonyPatch(typeof(StructureManager), "destroyStructure")]
            [HarmonyPrefix]
            [UsedImplicitly]
            internal static void DestroyStructure(StructureRegion region, ushort index)
            {
                ThreadUtil.assertIsGameThread();
                OnBuildableDestroyed?.Invoke(region.drops[index].instanceID, true);
            }
        }
    }
}