using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Buildables;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using UnityEngine;

#pragma warning disable 1591

namespace Pustalorc.Plugins.BaseClustering.Commands;

[UsedImplicitly]
public sealed class RemoveBuildableCommand : IRocketCommand
{
    public AllowedCaller AllowedCaller => AllowedCaller.Player;

    public string Name => "removebuildable";

    public string Help => "Removes the buildable you are staring at";

    public string Syntax => "";

    public List<string> Aliases => new();

    public List<string> Permissions => new() { "removebuildable" };

    public void Execute(IRocketPlayer caller, string[] command)
    {
        var pluginInstance = BaseClusteringPlugin.Instance;

        if (pluginInstance == null)
            throw new NullReferenceException("BaseClusteringPlugin.Instance is null. Cannot execute command.");

        if (caller is not UnturnedPlayer player) return;

        if (!Physics.Raycast(new Ray(player.Player.look.aim.position, player.Player.look.aim.forward), out var hit,
                player.Player.look.perspective == EPlayerPerspective.THIRD ? 6 : 4,
                RayMasks.BARRICADE_INTERACT | RayMasks.BARRICADE | RayMasks.STRUCTURE |
                RayMasks.STRUCTURE_INTERACT) ||
            hit.transform == null)
        {
            UnturnedChat.Say(caller, pluginInstance.Translate("not_looking_buildable"));
            return;
        }

        var buildable = BuildableDirectory.GetBuildable(hit.transform);

        if (buildable == null)
        {
            UnturnedChat.Say(caller, pluginInstance.Translate("not_looking_buildable"));
            return;
        }

        buildable.SafeDestroy();
    }
}