using System;
using System.Collections.Generic;
using System.Linq;
using Pustalorc.Plugins.BaseClustering.API.Buildables;
using Pustalorc.Plugins.BaseClustering.API.Utilities;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Pustalorc.Plugins.BaseClustering.Commands
{
    public sealed class TeleportToBuildCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;

        public string Name => "teleporttobuild";

        public string Help => "Teleports you to a random buildable on the map based on filters.";

        public string Syntax => "b [player] | s [player] | v [player] | [player] [id]";

        public List<string> Aliases => new() {"tpb"};

        public List<string> Permissions => new() {"teleporttobuild"};

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var pluginInstance = BaseClusteringPlugin.Instance;

            if (pluginInstance == null)
                throw new NullReferenceException("BaseClusteringPlugin.Instance is null. Cannot execute command.");

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

            var itemAssetInput = pluginInstance.Translate("not_available");
            var itemAssets = args.GetMultipleItemAssets(out index);
            var assetCount = itemAssets.Count;
            if (index > -1)
            {
                itemAssetInput = args[index];
                args.RemoveAt(index);
            }

            var builds = BuildableDirectory.GetBuildables(includePlants: plants);

            builds = target != null
                ? builds.Where(k => k.Owner.ToString().Equals(target.Id))
                : builds.Where(k => (k.Position - player.Position).sqrMagnitude > 400);

            if (barricades) builds = builds.Where(k => k.Asset is ItemBarricadeAsset);
            else if (structs) builds = builds.Where(k => k.Asset is ItemStructureAsset);

            if (assetCount > 0) builds = builds.Where(k => itemAssets.Exists(l => l.id == k.AssetId));

            var itemAssetName = pluginInstance.Translate("not_available");

            switch (assetCount)
            {
                case 1:
                    itemAssetName = itemAssets.First().itemName;
                    break;
                case > 1:
                    itemAssetName = itemAssetInput;
                    break;
            }

            var buildsL = builds.ToList();
            if (!buildsL.Any())
            {
                UnturnedChat.Say(caller, pluginInstance.Translate("cannot_teleport_no_builds", itemAssetName, target != null ? target.DisplayName : pluginInstance.Translate("not_available"), plants, barricades, structs));
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
                UnturnedChat.Say(caller, pluginInstance.Translate("cannot_teleport_builds_too_close", itemAssetName, target != null ? target.DisplayName : pluginInstance.Translate("not_available"), plants, barricades, structs));
            }
        }
    }
}