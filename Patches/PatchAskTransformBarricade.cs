// ReSharper disable InconsistentNaming

using HarmonyLib;
using Pustalorc.Plugins.BaseClustering.API.Delegates;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.Patches
{
    [HarmonyPatch(typeof(BarricadeManager), "askTransformBarricade")]
    public static class PatchAskTransformBarricade
    {
        public static event BuildableTransformed OnBarricadeTransformed;

        [HarmonyPrefix]
        public static bool AskTransformBarricade(CSteamID steamID, byte x, byte y, ushort plant, uint instanceID,
            Vector3 point, byte angle_x, byte angle_y, byte angle_z)
        {
            ThreadUtil.assertIsGameThread();

            if (!Provider.isServer || !BarricadeManager.tryGetRegion(x, y, plant, out _))
                return false;

            var player = PlayerTool.getPlayer(steamID);
            if (player == null || player.life.isDead || !player.look.canUseWorkzone) return false;

            var flag = true;
            if (BarricadeManager.onTransformRequested != null)
                BarricadeManager.onTransformRequested(steamID, x, y, plant, instanceID, ref point, ref angle_x,
                    ref angle_y, ref angle_z, ref flag);

            if (!flag) return false;

            if (plant == 65535)
                BarricadeManager.instance.channel.send("tellTransformBarricade", ESteamCall.ALL, x, y,
                    BarricadeManager.BARRICADE_REGIONS, ESteamPacket.UPDATE_RELIABLE_BUFFER, x, y, plant, instanceID,
                    point, angle_x, angle_y, angle_z);
            else
                BarricadeManager.instance.channel.send("tellTransformBarricade", ESteamCall.ALL,
                    ESteamPacket.UPDATE_RELIABLE_BUFFER, x, y, plant, instanceID, point, angle_x, angle_y, angle_z);

            OnBarricadeTransformed?.Invoke(instanceID);
            return false;
        }
    }
}