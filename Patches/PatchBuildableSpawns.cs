using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Classes;
using Pustalorc.Plugins.BaseClustering.API.Delegates;
using SDG.Unturned;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace Pustalorc.Plugins.BaseClustering.Patches
{
    [HarmonyPatch]
    public static class PatchBuildableSpawns
    {
        public static event BuildableSpawned OnBuildableSpawned;

        [HarmonyPatch(typeof(BarricadeManager), "dropBarricadeIntoRegionInternal")]
        [HarmonyPostfix]
        [UsedImplicitly]
        private static void DropBarricade(BarricadeRegion region, BarricadeData data, ref Transform result, ref uint instanceID)
        {
            if (result == null) return;

            var drop = region.drops.LastOrDefault();

            if (drop?.instanceID == instanceID)
            {
                OnBuildableSpawned?.Invoke(new Buildable(data, drop));
            }
        }

        [HarmonyPatch(typeof(StructureManager), "dropReplicatedStructure")]
        [HarmonyPostfix]
        [UsedImplicitly]
        private static void DropStructure(Vector3 point, bool __result, uint ___instanceCount)
        {
            if (!__result) return;

            if (!Regions.tryGetCoordinate(point, out var b, out var b2)) return;

            if (!StructureManager.tryGetRegion(b, b2, out var region)) return;

            var data = region.structures.LastOrDefault();
            var drop = region.drops.LastOrDefault();

            if (data?.instanceID == ___instanceCount && drop?.instanceID == ___instanceCount)
            {
                OnBuildableSpawned?.Invoke(new Buildable(data, drop));
            }
        }
    }
}