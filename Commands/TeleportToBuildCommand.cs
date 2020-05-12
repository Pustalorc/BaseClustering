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

            var id = args.GetUshort(out index);
            if (index > -1)
                args.RemoveAt(index);

            var builds = plants ? ReadOnlyGame.GetBuilds(CSteamID.Nil, true) : BaseClusteringPlugin.Instance.Buildables;

            if (target != null) builds = builds.Where(k => k.Owner.ToString().Equals(target.Id));

            if (barricades) builds = builds.Where(k => k.Asset is ItemBarricadeAsset);
            else if (structs) builds = builds.Where(k => k.Asset is ItemStructureAsset);

            if (id != ushort.MaxValue) builds = builds.Where(k => k.AssetId == id);

            var buildsL = builds.ToList();
            if (!buildsL.Any())
            {
                UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("cannot_teleport_no_builds"));
                return;
            }

            var build = buildsL[Random.Range(0, buildsL.Count - 1)];
            player.Teleport(
                new Vector3(build.Position.x, plants ? build.Position.y + 4 : build.Position.y + 2, build.Position.z),
                player.Rotation);
        }
    }
}