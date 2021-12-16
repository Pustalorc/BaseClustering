using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Buildables;
using Pustalorc.Plugins.BaseClustering.API.Utilities;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using UnityEngine;

#pragma warning disable 1591

namespace Pustalorc.Plugins.BaseClustering.Commands
{
    [UsedImplicitly]
    public sealed class FindBuildsCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public string Name => "findbuilds";

        public string Help => "Finds buildables around the map";

        public string Syntax =>
            "b [radius] | s [radius] | [id] [radius] | v [id] [radius] | [player] [id] [radius] | [player] b [radius] | [player] s [radius] | [player] v [id] [radius]";

        public List<string> Aliases => new List<string> { "fb" };

        public List<string> Permissions => new List<string> { "findbuilds" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var pluginInstance = BaseClusteringPlugin.Instance;

            if (pluginInstance == null)
                throw new NullReferenceException("BaseClusteringPlugin.Instance is null. Cannot execute command.");

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

            var radius = args.GetFloat(out index);
            if (index > -1)
                args.RemoveAt(index);

            var builds = BuildableDirectory.GetBuildables(includePlants: plants);

            if (target != null) builds = builds.Where(k => k.Owner.ToString().Equals(target.Id));

            if (barricades) builds = builds.Where(k => k.Asset is ItemBarricadeAsset);
            else if (structs) builds = builds.Where(k => k.Asset is ItemStructureAsset);

            if (assetCount > 0) builds = builds.Where(k => itemAssets.Exists(l => k.AssetId == l.id));

            if (!float.IsNegativeInfinity(radius))
            {
                if (!(caller is UnturnedPlayer cPlayer))
                {
                    UnturnedChat.Say(caller, pluginInstance.Translate("cannot_be_executed_from_console"));
                    return;
                }

                builds = builds.Where(k => (k.Position - cPlayer.Position).sqrMagnitude <= Mathf.Pow(radius, 2));
            }

            var itemAssetName = pluginInstance.Translate("not_available");

            if (assetCount == 1)
                itemAssetName = itemAssets.First().itemName;
            else if (assetCount > 1)
                itemAssetName = itemAssetInput;

            UnturnedChat.Say(caller,
                pluginInstance.Translate("build_count", builds.Count(), itemAssetName,
                    !float.IsNegativeInfinity(radius)
                        ? radius.ToString(CultureInfo.CurrentCulture)
                        : pluginInstance.Translate("not_available"),
                    target != null ? target.DisplayName : pluginInstance.Translate("not_available"), plants, barricades,
                    structs));
        }
    }
}