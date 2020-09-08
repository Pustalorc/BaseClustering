using HarmonyLib;
using Pustalorc.Plugins.BaseClustering.API.Delegates;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.Patches
{
    [HarmonyPatch(typeof(StructureManager), "askTransformStructure")]
    public static class PatchAskTransformStructure
    {
        public static event BuildableTransformed OnStructureTransformed;

        [HarmonyPrefix]
        public static bool AskTransformStructure(CSteamID steamID, byte x, byte y, uint instanceID, Vector3 point,
            byte angleX, byte angleY, byte angleZ)
        {
            if (!Provider.isServer || !StructureManager.tryGetRegion(x, y, out _)) return false;

            var player = PlayerTool.getPlayer(steamID);
            if (player == null || player.life.isDead || !player.look.canUseWorkzone) return false;

            var flag = true;
            if (StructureManager.onTransformRequested != null)
                StructureManager.onTransformRequested(steamID, x, y, instanceID, ref point, ref angleX, ref angleY,
                    ref angleZ, ref flag);

            if (!flag) return false;

            StructureManager.instance.channel.send("tellTransformStructure", ESteamCall.ALL, x, y,
                StructureManager.STRUCTURE_REGIONS, ESteamPacket.UPDATE_RELIABLE_BUFFER, x, y, instanceID, point,
                angleX, angleY, angleZ);

            OnStructureTransformed?.Invoke(instanceID);
            return false;
        }
    }
}