using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Delegates;
using SDG.Unturned;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.Patches
{
    [HarmonyPatch(typeof(StructureManager), "destroyStructure")]
    public static class PatchStructureDestroy
    {
        public static event BuildableDestroyed OnStructureDestroyed;

        [HarmonyPrefix]
        public static bool DestroyStructure([NotNull] StructureRegion region, byte x, byte y, ushort index,
            Vector3 ragdoll)
        {
            using (new StructureRegionSyncTest(region, "destroyStructure"))
            {
                var drop = region.drops.FirstOrDefault(k => k.instanceID == region.structures[index].instanceID);

                if (drop != null) OnStructureDestroyed?.Invoke(drop.model);

                region.structures.RemoveAt(index);
                StructureManager.instance.channel.send("tellTakeStructure", ESteamCall.ALL, x, y,
                    StructureManager.STRUCTURE_REGIONS, ESteamPacket.UPDATE_RELIABLE_BUFFER, x, y, index, ragdoll);
            }

            return false;
        }
    }
}