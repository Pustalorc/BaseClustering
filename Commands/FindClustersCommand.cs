using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Utilities;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using UnityEngine;

#pragma warning disable 1591

namespace Pustalorc.Plugins.BaseClustering.Commands
{
    [UsedImplicitly]
    public sealed class FindClustersCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "findclusters";
        public string Help => "Finds clusters around the map";
        public string Syntax => "<player> [id] [radius] | [id] [radius]";
        public List<string> Aliases => new List<string> {"fc"};
        public List<string> Permissions => new List<string> {"findclusters"};

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var pluginInstance = BaseClusteringPlugin.Instance;

            if (pluginInstance == null)
                throw new NullReferenceException("BaseClusteringPlugin.Instance is null. Cannot execute command.");

            var clusterDirectory = pluginInstance.BaseClusterDirectory;
            if (clusterDirectory == null)
            {
                UnturnedChat.Say(caller, pluginInstance.Translate("command_fail_clustering_disabled"));
                return;
            }

            var args = command.ToList();

            var target = args.GetIRocketPlayer(out var index);
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

            var clusters = target == null
                ? clusterDirectory.Clusters
                : clusterDirectory.GetClustersWithFilter(k =>
                    k.Buildables.Any(l => l.Owner.ToString().Equals(target.Id)));

            if (assetCount > 0)
                clusters = clusters.Where(k => k.Buildables.Any(l => itemAssets.Exists(z => l.AssetId == z.id)));

            if (!float.IsNegativeInfinity(radius))
            {
                if (!(caller is UnturnedPlayer cPlayer))
                {
                    UnturnedChat.Say(caller,
                        pluginInstance.Translate("cannot_be_executed_from_console"));
                    return;
                }

                clusters = clusters.Where(k =>
                    k.Buildables.Any(l => (l.Position - cPlayer.Position).sqrMagnitude <= Mathf.Pow(radius, 2)));
            }

            var itemAssetName = pluginInstance.Translate("not_available");

            if (assetCount == 1)
                itemAssetName = itemAssets.First().itemName;
            else if (assetCount > 1)
                itemAssetName = itemAssetInput;

            UnturnedChat.Say(caller,
                pluginInstance.Translate("cluster_count", clusters.Count(), itemAssetName,
                    !float.IsNegativeInfinity(radius)
                        ? radius.ToString(CultureInfo.CurrentCulture)
                        : pluginInstance.Translate("not_available"),
                    target != null ? target.DisplayName : pluginInstance.Translate("not_available")));
        }
    }
}