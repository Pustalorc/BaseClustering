using System.Collections.Generic;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Buildables;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.Commands
{
    public sealed class RemoveBuildableCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;

        [NotNull] public string Name => "removebuildable";

        [NotNull] public string Help => "Removes the buildable you are staring at";

        [NotNull] public string Syntax => "";

        [NotNull] public List<string> Aliases => new List<string>();

        [NotNull] public List<string> Permissions => new List<string> {"removebuildable"};

        public void Execute(IRocketPlayer caller, [NotNull] string[] command)
        {
            if (!(caller is UnturnedPlayer player)) return;

            if (!Physics.Raycast(new Ray(player.Player.look.aim.position, player.Player.look.aim.forward), out var hit,
                    player.Player.look.perspective == EPlayerPerspective.THIRD ? 6 : 4,
                    RayMasks.BARRICADE_INTERACT | RayMasks.BARRICADE | RayMasks.STRUCTURE |
                    RayMasks.STRUCTURE_INTERACT) ||
                hit.transform == null)
            {
                UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("not_looking_buildable"));
                return;
            }

            var buildable = BuildableDirectory.GetBuildable(hit.transform);

            if (buildable == null)
            {
                UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("not_looking_buildable"));
                return;
            }

            buildable.SafeDestroy();
        }
    }
}