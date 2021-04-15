using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Buildables;
using Pustalorc.Plugins.BaseClustering.API.Delegates;
using SDG.Unturned;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace Pustalorc.Plugins.BaseClustering.API.Patches
{
    public static class PatchBuildableSpawns
    {
        public static event BuildableChange OnBuildableSpawned;

        [HarmonyPatch]
        internal static class InternalPatches
        {
            [HarmonyPatch(typeof(BarricadeManager), "dropBarricadeIntoRegionInternal")]
            [HarmonyPostfix]
            [UsedImplicitly]
            internal static void DropBarricade(BarricadeRegion region, BarricadeData data, ref Transform result,
                ref uint instanceID)
            {
                if (result == null) return;

                var drop = region.drops.LastOrDefault();

                if (drop?.instanceID == instanceID) OnBuildableSpawned?.Invoke(new BarricadeBuildable(data, drop));
            }

            [HarmonyPatch(typeof(StructureManager), "dropReplicatedStructure")]
            [HarmonyPostfix]
            [UsedImplicitly]
            internal static void DropStructure(Vector3 point, bool __result, uint ___instanceCount)
            {
                if (!__result) return;

                if (!Regions.tryGetCoordinate(point, out var b, out var b2)) return;

                if (!StructureManager.tryGetRegion(b, b2, out var region)) return;

                var data = region.structures.LastOrDefault();
                var drop = region.drops.LastOrDefault();

                if (data?.instanceID == ___instanceCount && drop?.instanceID == ___instanceCount)
                    OnBuildableSpawned?.Invoke(new StructureBuildable(data, drop));
            }
        }
    }
}