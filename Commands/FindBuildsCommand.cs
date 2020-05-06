using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Statics;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using Steamworks;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.Commands
{
    public sealed class FindBuildsCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        [NotNull] public string Name => "findbuilds";
        [NotNull] public string Help => "Finds buildables around the map";
        [NotNull] public string Syntax => "vehicles [id] [radius] | [id] [radius]";
        [NotNull] public List<string> Aliases => new List<string> {"fb"};
        [NotNull] public List<string> Permissions => new List<string> {"findbuilds"};

        public void Execute(IRocketPlayer caller, [NotNull] string[] command)
        {
            switch (command.Length)
            {
                case 1:
                    if (!ushort.TryParse(command[0], out var id))
                    {
                        if (!command[0].Equals("vehicles", StringComparison.OrdinalIgnoreCase))
                        {
                            UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("not_valid_uint16", command[0]));
                            return;
                        }

                        UnturnedChat.Say(caller,
                            BaseClusteringPlugin.Instance.Translate("build_count",
                                ReadOnlyGame.GetBuilds(CSteamID.Nil, true).Count()));
                        return;
                    }

                    UnturnedChat.Say(caller,
                        BaseClusteringPlugin.Instance.Translate("build_count",
                            BaseClusteringPlugin.Instance.Buildables.Count(k => k.AssetId == id)));
                    return;
                case 2:
                    if (!ushort.TryParse(command[0], out id))
                    {
                        if (!command[0].Equals("vehicles", StringComparison.OrdinalIgnoreCase))
                        {
                            UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("not_valid_uint16", command[0]));
                            return;
                        }

                        if (!ushort.TryParse(command[1], out id))
                        {
                            UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("not_valid_uint16", command[1]));
                            return;
                        }

                        UnturnedChat.Say(caller,
                            BaseClusteringPlugin.Instance.Translate("build_count",
                                ReadOnlyGame.GetBuilds(CSteamID.Nil, true).Count(k => k.AssetId == id)));
                        return;
                    }

                    if (caller is ConsolePlayer || caller is RocketPlayer)
                    {
                        UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("cannot_be_executed_from_console"));
                        return;
                    }

                    var cPlayer = caller as UnturnedPlayer;

                    if (!float.TryParse(command[1], out var radius))
                    {
                        UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("not_valid_float", command[1]));
                        return;
                    }

                    UnturnedChat.Say(caller,
                        BaseClusteringPlugin.Instance.Translate("build_count",
                            BaseClusteringPlugin.Instance.Buildables.Count(k =>
                                // ReSharper disable once PossibleNullReferenceException
                                k.AssetId == id && Vector3.Distance(k.Position, cPlayer.Position) <= radius)));
                    return;
                case 3:
                    if (caller is ConsolePlayer || caller is RocketPlayer)
                    {
                        UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("cannot_be_executed_from_console"));
                        return;
                    }

                    cPlayer = caller as UnturnedPlayer;

                    if (!command[0].Equals("vehicles", StringComparison.OrdinalIgnoreCase))
                    {
                        UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("find_builds_wrong_argument"));
                        return;
                    }

                    if (!ushort.TryParse(command[1], out id))
                    {
                        UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("not_valid_uint16", command[1]));
                        return;
                    }

                    if (!float.TryParse(command[2], out radius))
                    {
                        UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("not_valid_float", command[2]));
                        return;
                    }

                    UnturnedChat.Say(caller,
                        BaseClusteringPlugin.Instance.Translate("build_count",
                            ReadOnlyGame.GetBuilds(CSteamID.Nil, true).Count(k =>
                                // ReSharper disable once PossibleNullReferenceException
                                k.AssetId == id && Vector3.Distance(k.Position, cPlayer.Position) <= radius)));
                    return;
                default:
                    UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("build_count", BaseClusteringPlugin.Instance.Buildables.Count));
                    return;
            }
        }
    }
}