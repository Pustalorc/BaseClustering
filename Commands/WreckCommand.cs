using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Commands;
using SDG.Framework.Translations;
using System;
using System.Collections.Generic;

namespace Pustalorc.Plugins.BaseClustering.Commands
{
    public sealed class WreckCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public string Name => "wreck";

        public string Help => "Destroys buildables from the map.";

        public string Syntax => "confirm | abort | b [radius] | s [radius] | <item> [radius] | v [item] [radius] | <player> [item] [radius]";

        public List<string> Aliases => new List<string> { "w" };

        public List<string> Permissions => new List<string> { "wreck" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            switch (command.Length)
            {
                case 1:
                    var arg1 = command[0];
                    var player = (IRocketPlayer) command.GetUnturnedPlayerParameter(0) ?? command.GetRocketPlayerParameter(0);
                    if (arg1.Equals("confirm", StringComparison.OrdinalIgnoreCase))
                    {
                        // Do wreck action
                    }
                    else if (arg1.Equals("abort", StringComparison.OrdinalIgnoreCase))
                    {
                        // Remove wreck action
                    }
                    else if (arg1.Equals("b", StringComparison.OrdinalIgnoreCase))
                    {
                        // Wreck all barricades
                    }
                    else if (arg1.Equals("s", StringComparison.OrdinalIgnoreCase))
                    {
                        // Wreck all structures
                    }
                    else if (arg1.Equals("v", StringComparison.OrdinalIgnoreCase))
                    {
                        // Wreck all barricades
                    }
                    else if (ushort.TryParse(arg1, out var id))
                    {
                        // Wreck all equaling ID
                    }
                    else if (player != null)
                    {
                        // Wreck all from user
                    }
                    return;
                case 2:
                    arg1 = command[0];
                    var arg2 = command[1];
                    player = (IRocketPlayer)command.GetUnturnedPlayerParameter(0) ?? command.GetRocketPlayerParameter(0);
                    if (arg1.Equals("confirm", StringComparison.OrdinalIgnoreCase))
                    {
                        UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("wreck_usage"));
                    }
                    else if (arg1.Equals("abort", StringComparison.OrdinalIgnoreCase))
                    {
                        UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("wreck_usage"));
                    }
                    else if (arg1.Equals("b", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!float.TryParse(arg2, out var range))
                        {
                            UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("wreck_usage"));
                            return;
                        }

                        // Wreck all barricades within range
                    }
                    else if (arg1.Equals("s", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!float.TryParse(arg2, out var range))
                        {
                            UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("wreck_usage"));
                            return;
                        }

                        // Wreck all structures within range
                    }
                    else if (arg1.Equals("v", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!float.TryParse(arg2, out var range))
                        {
                            UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("wreck_usage"));
                            return;
                        }

                        // Wreck all barricades within range
                    }
                    else if (ushort.TryParse(arg1, out var id))
                    {
                        if (!float.TryParse(arg2, out var range))
                        {
                            UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("wreck_usage"));
                            return;
                        }

                        // Wreck all equaling ID within range
                    }
                    else if (player != null)
                    {
                        if (!ushort.TryParse(arg2, out id))
                        {
                            if (!float.TryParse(arg2, out var range))
                            {
                                UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("wreck_usage"));
                                return;
                            }

                            // wreck all from user within range
                        }

                        // Wreck all from user of specific ID
                    }
                    return;
                default:
                    UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("wreck_usage"));
                    return;
            }
        }
    }
}
