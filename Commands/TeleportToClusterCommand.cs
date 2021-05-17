using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Utilities;
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
            var pluginInstance = BaseClusteringPlugin.Instance;
            var clusterDirectory = pluginInstance.BaseClusterDirectory;
            if (clusterDirectory == null)
            {
                UnturnedChat.Say(caller, pluginInstance.Translate("command_fail_clustering_disabled"));
                return;
            }

            if (!(caller is UnturnedPlayer player)) return;

            var args = command.ToList();

            var target = args.GetIRocketPlayer(out var index);
            if (index > -1)
                args.RemoveAt(index);

            var clusters = target != null
                ? clusterDirectory.GetClustersWithFilter(k =>
                    k.CommonOwner.ToString().Equals(target.Id))
                : clusterDirectory.Clusters;

            var clustersL = clusters.Where(k => k.AverageCenterPosition != Vector3.zero).ToList();
            if (!clustersL.Any())
            {
                UnturnedChat.Say(caller,
                    pluginInstance.Translate("cannot_teleport_no_clusters",
                        target != null
                            ? target.DisplayName
                            : pluginInstance.Translate("not_available")));
                return;
            }

            var cluster = clustersL[Random.Range(0, clustersL.Count - 1)];

            if (cluster != null)
            {
                var offset = new Vector3(0, 4, 0);

                while (!player.Player.stance.wouldHaveHeightClearanceAtPosition(cluster.AverageCenterPosition + offset,
                    0.5f))
                    offset.y++;

                player.Teleport(cluster.AverageCenterPosition + offset, player.Rotation);
            }
            else
            {
                UnturnedChat.Say(caller,
                    pluginInstance.Translate("cannot_teleport_no_clusters",
                        target != null
                            ? target.DisplayName
                            : pluginInstance.Translate("not_available")));
            }
        }
    }
}