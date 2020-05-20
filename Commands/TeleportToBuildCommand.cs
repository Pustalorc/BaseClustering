using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Statics;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.Commands
{
    public sealed class TeleportToBuildCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;

        [NotNull] public string Name => "teleporttobuild";

        [NotNull] public string Help => "Teleports you to a random buildable on the map based on filters.";

        [NotNull] public string Syntax => "b [player] | s [player] | v [player] | [player] [id]";

        [NotNull] public List<string> Aliases => new List<string> {"tpb"};

        [NotNull] public List<string> Permissions => new List<string> {"teleporttobuild"};

        public void Execute(IRocketPlayer caller, [NotNull] string[] command)
        {
            if (!(caller is UnturnedPlayer player)) return;

            var args = command.ToList();

            var barricades = args.CheckArgsIncludeString("b", out var index);
            if (index > -1)
                args.RemoveAt(index);

            var structs = args.CheckArgsIncludeString("s", out index);
            if (index > -1)
                args.RemoveAt(index);

            var plants = args.CheckArgsIncludeString("v", out index);
            if (index > -1)
                args.RemoveAt(index);

            var target = args.GetIRocketPlayer(out index);
            if (index > -1)
                args.RemoveAt(index);

            var itemAsset = args.GetItemAsset(out index);
            if (index > -1)
                args.RemoveAt(index);

            var builds = plants ? ReadOnlyGame.GetBuilds(CSteamID.Nil, true) : BaseClusteringPlugin.Instance.Buildables;

            builds = target != null
                ? builds.Where(k => k.Owner.ToString().Equals(target.Id))
                : builds.Where(k => Vector3.Distance(k.Position, player.Position) > 20);

            if (barricades) builds = builds.Where(k => k.Asset is ItemBarricadeAsset);
            else if (structs) builds = builds.Where(k => k.Asset is ItemStructureAsset);

            if (itemAsset != null) builds = builds.Where(k => k.AssetId == itemAsset.id);

            var buildsL = builds.ToList();
            if (!buildsL.Any())
            {
                UnturnedChat.Say(caller,
                    BaseClusteringPlugin.Instance.Translate("cannot_teleport_no_builds",
                        itemAsset != null
                            ? itemAsset.itemName
                            : BaseClusteringPlugin.Instance.Translate("not_available"),
                        target != null ? target.DisplayName : BaseClusteringPlugin.Instance.Translate("not_available"),
                        plants, barricades, structs));
                return;
            }

            var build = buildsL[Random.Range(0, buildsL.Count - 1)];

            if (build != null)
            {
                var offset = new Vector3(0, plants ? 4 : 2, 0);

                while (!player.Player.stance.wouldHaveHeightClearanceAtPosition(build.Position + offset, 0.5f))
                    offset.y++;

                player.Teleport(build.Position + offset, player.Rotation);
            }
            else
            {
                UnturnedChat.Say(caller,
                    BaseClusteringPlugin.Instance.Translate("cannot_teleport_builds_too_close",
                        itemAsset != null
                            ? itemAsset.itemName
                            : BaseClusteringPlugin.Instance.Translate("not_available"),
                        target != null ? target.DisplayName : BaseClusteringPlugin.Instance.Translate("not_available"),
                        plants, barricades, structs));
            }
        }
    }
}