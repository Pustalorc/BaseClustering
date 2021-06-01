using System;
using System.Collections.Generic;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.Commands
{
    public sealed class WreckVehicleCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;

        public string Name => "wreckvehicle";

        public string Help => "Wrecks all the buildables on the vehicle that you are looking at.";

        public string Syntax => "";

        public List<string> Aliases => new() {"wv"};

        public List<string> Permissions => new() {"wreckvehicle"};

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var player = (UnturnedPlayer) caller;
            var raycastInfo = DamageTool.raycast(new Ray(player.Player.look.aim.position, player.Player.look.aim.forward), 10f, RayMasks.VEHICLE);
            var baseClusteringPlugin = BaseClusteringPlugin.Instance;

            if (baseClusteringPlugin == null)
                throw new NullReferenceException("BaseClusteringPlugin.Instance is null. Cannot execute command.");

            if (raycastInfo.vehicle == null)
            {
                UnturnedChat.Say(caller, baseClusteringPlugin.Translate("no_vehicle_found"));
                return;
            }

            if (raycastInfo.vehicle.isDead)
            {
                UnturnedChat.Say(caller, baseClusteringPlugin.Translate("vehicle_dead"));
                return;
            }

            if (!BarricadeManager.tryGetPlant(raycastInfo.transform, out var x, out var y, out var plant,
                out var region))
            {
                UnturnedChat.Say(caller, baseClusteringPlugin.Translate("vehicle_no_plant"));
                return;
            }

            for (var i = region.barricades.Count - 1; i > 0; i--)
                BarricadeManager.destroyBarricade(region, x, y, plant, (ushort) i);

            UnturnedChat.Say(caller,
                baseClusteringPlugin.Translate("vehicle_wreck",
                    raycastInfo.vehicle.asset.vehicleName ?? raycastInfo.vehicle.asset.name,
                    raycastInfo.vehicle.id, raycastInfo.vehicle.instanceID,
                    raycastInfo.vehicle.lockedOwner.ToString()));
        }
    }
}