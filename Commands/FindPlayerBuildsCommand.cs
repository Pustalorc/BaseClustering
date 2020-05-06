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
    public sealed class FindPlayerBuildsCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        [NotNull] public string Name => "findplayerbuilds";
        [NotNull] public string Help => "Finds buildables around the map that belong to a player";
        [NotNull] public string Syntax => "<player> vehicles [id] [radius] | <player> [id] [radius]";
        [NotNull] public List<string> Aliases => new List<string> {"fpb"};
        [NotNull] public List<string> Permissions => new List<string> {"findplayerbuilds"};

        public void Execute(IRocketPlayer caller, [NotNull] string[] command)
        {
            switch (command.Length)
            {
                case 1:
                    var target = (IRocketPlayer) command.GetUnturnedPlayerParameter(0) ??
                                 command.GetRocketPlayerParameter(0);

                    if (target == null)
                    {
                        UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("not_valid_player", command[0]));
                        return;
                    }

                    UnturnedChat.Say(caller,
                        BaseClusteringPlugin.Instance.Translate("build_count",
                            BaseClusteringPlugin.Instance.Buildables.Count(k => k.Owner == ulong.Parse(target.Id))));
                    return;
                case 2:
                    target = (IRocketPlayer) command.GetUnturnedPlayerParameter(0) ??
                             command.GetRocketPlayerParameter(0);

                    if (target == null)
                    {
                        UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("not_valid_player", command[0]));
                        return;
                    }

                    if (!ushort.TryParse(command[1], out var id))
                    {
                        if (!command[1].Equals("vehicles", StringComparison.OrdinalIgnoreCase))
                        {
                            UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("not_valid_uint16", command[1]));
                            return;
                        }

                        UnturnedChat.Say(caller,
                            BaseClusteringPlugin.Instance.Translate("build_count",
                                ReadOnlyGame.GetBuilds(new CSteamID(ulong.Parse(target.Id)), true).Count()));
                        return;
                    }

                    UnturnedChat.Say(caller,
                        BaseClusteringPlugin.Instance.Translate("build_count",
                            BaseClusteringPlugin.Instance.Buildables.Count(k =>
                                k.AssetId == id && k.Owner == ulong.Parse(target.Id))));
                    return;
                case 3:
                    target = (IRocketPlayer) command.GetUnturnedPlayerParameter(0) ??
                             command.GetRocketPlayerParameter(0);

                    if (target == null)
                    {
                        UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("not_valid_player", command[0]));
                        return;
                    }

                    if (!ushort.TryParse(command[1], out id))
                    {
                        if (!command[1].Equals("vehicles", StringComparison.OrdinalIgnoreCase))
                        {
                            UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("not_valid_uint16", command[1]));
                            return;
                        }

                        if (!ushort.TryParse(command[2], out id))
                        {
                            UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("not_valid_uint16", command[2]));
                            return;
                        }

                        UnturnedChat.Say(caller,
                            BaseClusteringPlugin.Instance.Translate("build_count",
                                ReadOnlyGame.GetBuilds(new CSteamID(ulong.Parse(target.Id)), true)
                                    .Count(k => k.AssetId == id)));
                        return;
                    }

                    if (caller is ConsolePlayer || caller is RocketPlayer)
                    {
                        UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("cannot_be_executed_from_console"));
                        return;
                    }

                    var cPlayer = caller as UnturnedPlayer;

                    if (!float.TryParse(command[2], out var radius))
                    {
                        UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("not_valid_float", command[2]));
                        return;
                    }

                    UnturnedChat.Say(caller,
                        BaseClusteringPlugin.Instance.Translate("build_count",
                            BaseClusteringPlugin.Instance.Buildables.Count(k =>
                                k.AssetId == id && k.Owner == ulong.Parse(target.Id) &&
                                // ReSharper disable once PossibleNullReferenceException
                                Vector3.Distance(k.Position, cPlayer.Position) <= radius)));
                    return;
                case 4:
                    target = (IRocketPlayer) command.GetUnturnedPlayerParameter(0) ??
                             command.GetRocketPlayerParameter(0);

                    if (target == null)
                    {
                        UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("not_valid_player", command[0]));
                        return;
                    }

                    if (caller is ConsolePlayer || caller is RocketPlayer)
                    {
                        UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("cannot_be_executed_from_console"));
                        return;
                    }

                    cPlayer = caller as UnturnedPlayer;

                    if (!command[1].Equals("vehicles", StringComparison.OrdinalIgnoreCase))
                    {
                        UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("find_builds_wrong_argument"));
                        return;
                    }

                    if (!ushort.TryParse(command[2], out id))
                    {
                        UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("not_valid_uint16", command[2]));
                        return;
                    }

                    if (!float.TryParse(command[3], out radius))
                    {
                        UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("not_valid_float", command[3]));
                        return;
                    }

                    UnturnedChat.Say(caller,
                        BaseClusteringPlugin.Instance.Translate("build_count",
                            ReadOnlyGame.GetBuilds(new CSteamID(ulong.Parse(target.Id)), true).Count(k =>
                                // ReSharper disable once PossibleNullReferenceException
                                k.AssetId == id && Vector3.Distance(k.Position, cPlayer.Position) <= radius)));
                    return;
                default:
                    UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("fpb_usage"));
                    return;
            }
        }
    }
}