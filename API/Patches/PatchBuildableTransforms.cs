// ReSharper disable InconsistentNaming

using HarmonyLib;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Buildables;
using Pustalorc.Plugins.BaseClustering.API.Delegates;
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
                OnBuildableTransformed?.Invoke(BuildableDirectory.GetBuildable(instanceID, false));
            }

            [HarmonyPatch(typeof(StructureManager), "ReceiveTransformStructure")]
            [HarmonyPostfix]
            [UsedImplicitly]
            internal static void ReceiveTransformStructure(uint instanceID)
            {
                OnBuildableTransformed?.Invoke(BuildableDirectory.GetBuildable(instanceID, true));
            }
        }
    }
}