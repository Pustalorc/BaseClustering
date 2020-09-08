using HarmonyLib;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Delegates;
using SDG.Unturned;

namespace Pustalorc.Plugins.BaseClustering.Patches
{
    [HarmonyPatch(typeof(BarricadeManager), "destroyBarricade")]
    public static class PatchBarricadeDestroy
    {
        public static event BuildableDestroyed OnBarricadeDestroyed;

        [HarmonyPrefix]
        public static bool DestroyBarricade([NotNull] BarricadeRegion region, byte x, byte y, ushort plant,
            ushort index)
        {
            ThreadUtil.assertIsGameThread();

            var drop = region.drops[index];
            if (drop != null) OnBarricadeDestroyed?.Invoke(drop.model);

            region.barricades.RemoveAt(index);
            if (plant == 65535)
                BarricadeManager.instance.channel.send("tellTakeBarricade", ESteamCall.ALL, x, y,
                    BarricadeManager.BARRICADE_REGIONS, ESteamPacket.UPDATE_RELIABLE_BUFFER, x, y, plant, index);
            else
                BarricadeManager.instance.channel.send("tellTakeBarricade", ESteamCall.ALL,
                    ESteamPacket.UPDATE_RELIABLE_BUFFER, x, y, plant, index);

            return false;
        }
    }
}