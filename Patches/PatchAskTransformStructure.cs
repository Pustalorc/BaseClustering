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
        public static bool AskTransformStructure(CSteamID steamID, byte x, byte y, uint instanceID, Vector3 point, byte angle_x, byte angle_y, byte angle_z)
        {
            if (!Provider.isServer || !StructureManager.tryGetRegion(x, y, out _)) return false;

            var player = PlayerTool.getPlayer(steamID);
            if (player == null || player.life.isDead || !player.look.canUseWorkzone) return false;

            var flag = true;
            if (StructureManager.onTransformRequested != null)
                StructureManager.onTransformRequested(steamID, x, y, instanceID, ref point, ref angle_x, ref angle_y,
                    ref angle_z, ref flag);

            if (!flag) return false;

            StructureManager.instance.channel.send("tellTransformStructure", ESteamCall.ALL, x, y,
                StructureManager.STRUCTURE_REGIONS, ESteamPacket.UPDATE_RELIABLE_BUFFER, x, y, instanceID, point,
                angle_x, angle_y, angle_z);

            OnStructureTransformed?.Invoke(instanceID);
            return false;
        }
    }
}