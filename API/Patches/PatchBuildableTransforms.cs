using System.Linq;
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
            [HarmonyPatch(typeof(BarricadeManager), "ReceiveTransformBarricade")]
            [HarmonyPostfix]
            [UsedImplicitly]
            internal static void ReceiveTransformBarricade(byte x, byte y, ushort plant, uint instanceID)
            {
                var buildable = BuildableDirectory.GetBuildable(instanceID, false);

                if (buildable == null)
                {
                    if (!BarricadeManager.tryGetRegion(x, y, plant, out var region))
                        return;

                    var data = region.barricades.FirstOrDefault(k => k.instanceID == instanceID);
                    var drop = region.drops.FirstOrDefault(k => k.instanceID == instanceID);

                    if (data == null || drop == null)
                        return;

                    buildable = new BarricadeBuildable(data, drop);
                }

                OnBuildableTransformed?.Invoke(buildable!);
            }

            [HarmonyPatch(typeof(StructureManager), "ReceiveTransformStructure")]
            [HarmonyPostfix]
            [UsedImplicitly]
            internal static void ReceiveTransformStructure(byte x, byte y, uint instanceID)
            {
                var buildable = BuildableDirectory.GetBuildable(instanceID, true);

                if (buildable == null)
                {
                    if (!StructureManager.tryGetRegion(x, y, out var region))
                        return;

                    var data = region.structures.FirstOrDefault(k => k.instanceID == instanceID);
                    var drop = region.drops.FirstOrDefault(k => k.instanceID == instanceID);

                    if (data == null || drop == null)
                        return;

                    buildable = new StructureBuildable(data, drop);
                }

                OnBuildableTransformed?.Invoke(buildable!);
            }
        }
    }
}