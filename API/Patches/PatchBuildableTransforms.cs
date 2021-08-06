using HarmonyLib;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Buildables;
using Pustalorc.Plugins.BaseClustering.API.Delegates;
using SDG.Unturned;

// ReSharper disable InconsistentNaming

namespace Pustalorc.Plugins.BaseClustering.API.Patches
{
    /// <summary>
    /// A patch for barricades and structures being transformed.
    /// </summary>
    public static class PatchBuildableTransforms
    {
        /// <summary>
        /// Event is fired whenever a barricade or structure is transformed.
        /// </summary>
        public static event BuildableChange? OnBuildableTransformed;

        [HarmonyPatch]
        internal static class InternalPatches
        {
            [HarmonyPatch(typeof(BarricadeDrop), "ReceiveTransform")]
            [HarmonyPostfix]
            [UsedImplicitly]
            internal static void ReceiveTransformBarricade(BarricadeDrop __instance)
            {
                var buildable = BuildableDirectory.GetBuildable(__instance.instanceID, false) ??
                                new BarricadeBuildable(__instance);

                OnBuildableTransformed?.Invoke(buildable!);
            }

            [HarmonyPatch(typeof(StructureDrop), "ReceiveTransform")]
            [HarmonyPostfix]
            [UsedImplicitly]
            internal static void ReceiveTransformStructure(StructureDrop __instance)
            {
                var buildable = BuildableDirectory.GetBuildable(__instance.instanceID, true) ??
                                new StructureBuildable(__instance);

                OnBuildableTransformed?.Invoke(buildable!);
            }
        }
    }
}