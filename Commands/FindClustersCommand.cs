using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Statics;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Commands;
using Rocket.Unturned.Player;
using Steamworks;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.Commands
{
    public sealed class FindClustersCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        [NotNull] public string Name => "findclusters";
        [NotNull] public string Help => "Finds clusters around the map";
        [NotNull] public string Syntax => "<player> [id] [radius] | [id] [radius]";
        [NotNull] public List<string> Aliases => new List<string> { "fc" };
        [NotNull] public List<string> Permissions => new List<string> { "findclusters" };

        public void Execute(IRocketPlayer caller, [NotNull] string[] command)
        {
            switch (command.Length)
            {
                case 1:
                    var target = (IRocketPlayer)command.GetUnturnedPlayerParameter(0) ?? command.GetRocketPlayerParameter(0);

                    if (target == null)
                    {
                        if (!ushort.TryParse(command[0], out var id))
                        {
                            if (!float.TryParse(command[0], out var range))
                            {
                                UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("not_valid_player_or_float", command[0]));
                                return;
                            }

                            if (!(caller is UnturnedPlayer cPlayer))
                            {
                                UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("cannot_be_executed_from_console"));
                                return;
                            }

                            UnturnedChat.Say(caller,
                                BaseClusteringPlugin.Instance.Translate("cluster_count",
                                    BaseClusteringPlugin.Instance.Clusters.Count(k => Vector3.Distance(k.CenterBuildable, cPlayer.position
                                    ))));
                            return;
                        }

                        UnturnedChat.Say(caller,
                            BaseClusteringPlugin.Instance.Translate("cluster_count",
                                BaseClusteringPlugin.Instance.Clusters.Count(k => k.Buildables.Any(l => l.AssetId == id))));
                        return;
                    }

                    UnturnedChat.Say(caller,
                        BaseClusteringPlugin.Instance.Translate("cluster_count",
                            BaseClusteringPlugin.Instance.Clusters.Count(k => k.Buildables.Any(l => l.Owner.ToString().Equals(target.Id)))));
                    return;
                case 2:
                    target = (IRocketPlayer)command.GetUnturnedPlayerParameter(0) ?? command.GetRocketPlayerParameter(0);

                    if (target == null)
                    {
                        if (!ushort.TryParse(command[0], out var id))
                        {
                            UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("not_valid_player_or_uint16", command[0]));
                            return;
                        }

                        if (!float.TryParse(command[1], out var range))
                        {
                            UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("not_valid_float", command[0]));
                            return;
                        }

                        UnturnedChat.Say(caller,
                            BaseClusteringPlugin.Instance.Translate("cluster_count",
                                BaseClusteringPlugin.Instance.Clusters.Count(k => k.Buildables.Any(l => l.AssetId == id))));
                        return;
                    }

                    UnturnedChat.Say(caller,
                        BaseClusteringPlugin.Instance.Translate("cluster_count",
                            BaseClusteringPlugin.Instance.Clusters.Count(k => k.Buildables.Any(l => l.Owner.ToString().Equals(target.Id)))));
                    return;
                default:
                    UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("cluster_count", BaseClusteringPlugin.Instance.Clusters.Count));
                    return;
            }
        }
    }
}