using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Utilities;
using Pustalorc.Plugins.BaseClustering.API.WreckingActions;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using UnityEngine;

#pragma warning disable 1591

namespace Pustalorc.Plugins.BaseClustering.Commands
{
    [UsedImplicitly]
    public sealed class WreckClustersCommand : IRocketCommand
    {
        private readonly Dictionary<string, WreckClustersAction> m_WreckActions =
            new Dictionary<string, WreckClustersAction>();

        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public string Name => "wreckclusters";

        public string Help => "Destroys clusters from the map.";

        public string Syntax => "confirm | abort | [player] [item] [radius]";

        public List<string> Aliases => new List<string> {"wc"};

        public List<string> Permissions => new List<string> {"wreckclusters"};

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

            var cId = caller.Id;
            var args = command.ToList();

            if (args.Count == 0)
            {
                UnturnedChat.Say(caller, pluginInstance.Translate("not_enough_args"));
                return;
            }

            var abort = args.CheckArgsIncludeString("abort", out var index);
            if (index > -1)
                args.RemoveAt(index);

            var confirm = args.CheckArgsIncludeString("confirm", out index);
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

            if (abort)
            {
                if (m_WreckActions.TryGetValue(cId, out _))
                {
                    m_WreckActions.Remove(cId);
                    UnturnedChat.Say(caller, pluginInstance.Translate("action_cancelled"));
                    return;
                }

                UnturnedChat.Say(caller, pluginInstance.Translate("no_action_queued"));
                return;
            }

            if (confirm)
            {
                if (!m_WreckActions.TryGetValue(cId, out var action))
                {
                    UnturnedChat.Say(caller, pluginInstance.Translate("no_action_queued"));
                    return;
                }

                m_WreckActions.Remove(cId);

                var remove = action.TargetPlayer != null
                    ? clusterDirectory.GetClustersWithFilter(k =>
                        k.CommonOwner.ToString().Equals(action.TargetPlayer.Id))
                    : clusterDirectory.Clusters;

                if (action.ItemAssets.Count > 0)
                    remove = remove.Where(k => k.Buildables.Any(l => action.ItemAssets.Exists(z => l.AssetId == z.id)));

                if (!action.Center.IsNegativeInfinity())
                    remove = remove.Where(k =>
                        k.Buildables.Any(l =>
                            (l.Position - action.Center).sqrMagnitude <= Mathf.Pow(action.Radius, 2)));

                var baseClusters = remove.ToList();
                if (!baseClusters.Any())
                {
                    UnturnedChat.Say(caller, pluginInstance.Translate("cannot_wreck_no_clusters"));
                    return;
                }

                foreach (var cluster in baseClusters)
                    cluster.Destroy();

                UnturnedChat.Say(caller,
                    pluginInstance.Translate("wrecked_clusters", baseClusters.Count, action.ItemAssetName,
                        !float.IsNegativeInfinity(action.Radius)
                            ? action.Radius.ToString(CultureInfo.CurrentCulture)
                            : pluginInstance.Translate("not_available"),
                        action.TargetPlayer != null
                            ? action.TargetPlayer.DisplayName
                            : pluginInstance.Translate("not_available")));
                return;
            }

            var clusters = target != null
                ? clusterDirectory.GetClustersWithFilter(k =>
                    k.CommonOwner.ToString().Equals(target.Id))
                : clusterDirectory.Clusters;

            if (assetCount > 0)
                clusters = clusters.Where(k => k.Buildables.Any(l => itemAssets.Exists(z => l.AssetId == z.id)));

            var center = Vector3.negativeInfinity;

            if (!float.IsNegativeInfinity(radius))
            {
                if (!(caller is UnturnedPlayer cPlayer))
                {
                    UnturnedChat.Say(caller,
                        pluginInstance.Translate("cannot_be_executed_from_console"));
                    return;
                }

                center = cPlayer.Position;
                clusters = clusters.Where(k =>
                    k.Buildables.Any(l => (l.Position - center).sqrMagnitude <= Mathf.Pow(radius, 2)));
            }

            var count = clusters.Count();

            if (count <= 0)
            {
                UnturnedChat.Say(caller, pluginInstance.Translate("cannot_wreck_no_clusters"));
                return;
            }

            var itemAssetName = pluginInstance.Translate("not_available");

            if (assetCount == 1)
                itemAssetName = itemAssets.First().itemName;
            else if (assetCount > 1)
                itemAssetName = itemAssetInput;

            if (m_WreckActions.TryGetValue(cId, out _))
            {
                m_WreckActions[cId] = new WreckClustersAction(target, center, itemAssets, radius, itemAssetInput);
                UnturnedChat.Say(caller,
                    pluginInstance.Translate("wreck_clusters_action_queued_new",
                        target?.DisplayName ?? pluginInstance.Translate("not_available"), itemAssetName,
                        !float.IsNegativeInfinity(radius)
                            ? radius.ToString(CultureInfo.CurrentCulture)
                            : pluginInstance.Translate("not_available"), count));
            }
            else
            {
                m_WreckActions.Add(cId, new WreckClustersAction(target, center, itemAssets, radius, itemAssetInput));
                UnturnedChat.Say(caller,
                    pluginInstance.Translate("wreck_clusters_action_queued",
                        target?.DisplayName ?? pluginInstance.Translate("not_available"), itemAssetName,
                        !float.IsNegativeInfinity(radius)
                            ? radius.ToString(CultureInfo.CurrentCulture)
                            : pluginInstance.Translate("not_available"), count));
            }
        }
    }
}