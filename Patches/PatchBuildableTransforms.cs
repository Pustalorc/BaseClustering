// ReSharper disable InconsistentNaming

using HarmonyLib;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Delegates;
using SDG.Unturned;

namespace Pustalorc.Plugins.BaseClustering.Patches
{
    public static class PatchBuildableTransforms
    {
        public static event BuildableTransformed OnBuildableTransformed;

        [HarmonyPatch]
        public static class InternalPatches
        {
            [HarmonyPatch(typeof(BarricadeManager), "askTransformBarricade")]
            [HarmonyPostfix]
            [UsedImplicitly]
            public static void AskTransformBarricade(uint instanceID)
            {
                OnBuildableTransformed?.Invoke(instanceID);
            }

            [HarmonyPatch(typeof(StructureManager), "askTransformStructure")]
            [HarmonyPostfix]
            [UsedImplicitly]
            public static void AskTransformStructure(uint instanceID)
            {
                OnBuildableTransformed?.Invoke(instanceID);
            }
        }
    }
}