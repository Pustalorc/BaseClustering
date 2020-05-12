﻿using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Statics;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.Commands
{
    public sealed class TeleportToClusterCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;

        [NotNull] public string Name => "teleporttocluster";

        [NotNull] public string Help => "Teleports you to a random cluster on the map based on filters.";

        [NotNull] public string Syntax => "[player]";

        [NotNull] public List<string> Aliases => new List<string> {"tpc"};

        [NotNull] public List<string> Permissions => new List<string> {"teleporttocluster"};

        public void Execute(IRocketPlayer caller, [NotNull] string[] command)
        {
            if (!(caller is UnturnedPlayer player)) return;

            var args = command.ToList();

            var target = args.GetIRocketPlayer(out var index);
            if (index > -1)
                args.RemoveAt(index);

            var clusters = target != null
                ? BaseClusteringPlugin.Instance.Clusters.Where(k => k.CommonOwner.ToString().Equals(target.Id))
                : BaseClusteringPlugin.Instance.Clusters;

            var clustersL = clusters.ToList();
            if (!clustersL.Any())
            {
                UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("cannot_teleport_no_builds"));
                return;
            }

            var cluster = clustersL[Random.Range(0, clustersL.Count - 1)];
            player.Teleport(
                new Vector3(cluster.AverageCenterPosition.x, cluster.AverageCenterPosition.y + 4,
                    cluster.AverageCenterPosition.z), player.Rotation);
        }
    }
}