using System.Collections.Generic;
using JetBrains.Annotations;
using PlayerInfoLibrary;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.Commands
{
    public sealed class WreckVehicleCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;

        [NotNull] public string Name => "wreckvehicle";

        [NotNull] public string Help => "Wrecks all the buildables on the vehicle that you are looking at.";

        [NotNull] public string Syntax => "";

        [NotNull] public List<string> Aliases => new List<string> {"wv"};

        [NotNull] public List<string> Permissions => new List<string> {"wreckvehicle"};

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var player = (UnturnedPlayer) caller;
            var raycastInfo =
                DamageTool.raycast(new Ray(player.Player.look.aim.position, player.Player.look.aim.forward), 10f,
                    RayMasks.VEHICLE);

            if (raycastInfo.vehicle == null)
            {
                UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("no_vehicle_found"));
                return;
            }

            if (raycastInfo.vehicle.isDead)
            {
                UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("vehicle_dead"));
                return;
            }

            var id = raycastInfo.vehicle.lockedOwner;
            var pId = PlayerInfoLib.Instance?.database?.QueryById(id) ?? null;

            if (!BarricadeManager.tryGetPlant(raycastInfo.transform, out var x, out var y, out var plant,
                out var region))
            {
                UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("vehicle_no_plant"));
                return;
            }

            for (var i = region.barricades.Count - 1; i > 0; i--)
                BarricadeManager.destroyBarricade(region, x, y, plant, (ushort) i);

            UnturnedChat.Say(caller,
                BaseClusteringPlugin.Instance.Translate("vehicle_wreck", raycastInfo.vehicle.asset.name,
                    raycastInfo.vehicle.id, raycastInfo.vehicle.instanceID,
                    id == CSteamID.Nil ? "N/A" : pId?.CharacterName ?? raycastInfo.vehicle.lockedOwner.ToString()));
        }
    }
}