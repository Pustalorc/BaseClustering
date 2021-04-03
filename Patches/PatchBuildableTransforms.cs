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
            [HarmonyPatch(typeof(BarricadeManager), "ReceiveTransformBarricade")]
            [HarmonyPostfix]
            [UsedImplicitly]
            public static void ReceiveTransformBarricade(uint instanceID)
            {
                OnBuildableTransformed?.Invoke(instanceID);
            }

            [HarmonyPatch(typeof(StructureManager), "ReceiveTransformStructure")]
            [HarmonyPostfix]
            [UsedImplicitly]
            public static void ReceiveTransformStructure(uint instanceID)
            {
                OnBuildableTransformed?.Invoke(instanceID);
            }
        }
    }
}