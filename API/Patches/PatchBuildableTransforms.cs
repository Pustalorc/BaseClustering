// ReSharper disable InconsistentNaming

using HarmonyLib;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Delegates;
using SDG.Unturned;

namespace Pustalorc.Plugins.BaseClustering.API.Patches
{
    public static class PatchBuildableTransforms
    {
        public static event BuildableTransformed OnBuildableTransformed;

        [HarmonyPatch]
        internal static class InternalPatches
        {
            [HarmonyPatch(typeof(BarricadeManager), "askTransformBarricade")]
            [HarmonyPostfix]
            [UsedImplicitly]
            internal static void AskTransformBarricade(uint instanceID)
            {
                OnBuildableTransformed?.Invoke(instanceID, false);
            }

            [HarmonyPatch(typeof(StructureManager), "askTransformStructure")]
            [HarmonyPostfix]
            [UsedImplicitly]
            internal static void AskTransformStructure(uint instanceID)
            {
                OnBuildableTransformed?.Invoke(instanceID, true);
            }
        }
    }
}