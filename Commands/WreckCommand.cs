using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Buildables;
using Pustalorc.Plugins.BaseClustering.API.Utilities;
using Pustalorc.Plugins.BaseClustering.API.WreckingActions;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using UnityEngine;

#pragma warning disable 1591

namespace Pustalorc.Plugins.BaseClustering.Commands
{
    [UsedImplicitly]
    public sealed class WreckCommand : IRocketCommand
    {
        private readonly Dictionary<string, WreckAction> m_WreckActions = new();

        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public string Name => "wreck";

        public string Help => "Destroys buildables from the map.";

        public string Syntax =>
            "confirm | abort | b [radius] | s [radius] | <item> [radius] | v [item] [radius] | <player> [item] [radius]";

        public List<string> Aliases => new() {"w"};

        public List<string> Permissions => new() {"wreck"};

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var cId = caller.Id;
            var args = command.ToList();
            var baseClusteringPlugin = BaseClusteringPlugin.Instance;

            if (baseClusteringPlugin == null)
                throw new NullReferenceException("BaseClusteringPlugin.Instance is null. Cannot execute command.");

            if (args.Count == 0)
            {
                UnturnedChat.Say(caller, baseClusteringPlugin.Translate("not_enough_args"));
                return;
            }

            var abort = args.CheckArgsIncludeString("abort", out var index);
            if (index > -1)
                args.RemoveAt(index);

            var confirm = args.CheckArgsIncludeString("confirm", out index);
            if (index > -1)
                args.RemoveAt(index);

            var plants = args.CheckArgsIncludeString("v", out index);
            if (index > -1)
                args.RemoveAt(index);

            var barricades = args.CheckArgsIncludeString("b", out index);
            if (index > -1)
                args.RemoveAt(index);

            var structs = args.CheckArgsIncludeString("s", out index);
            if (index > -1)
                args.RemoveAt(index);

            var target = args.GetIRocketPlayer(out index);
            if (index > -1)
                args.RemoveAt(index);

            var itemAssetInput = baseClusteringPlugin.Translate("not_available");
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
                    UnturnedChat.Say(caller, baseClusteringPlugin.Translate("action_cancelled"));
                    return;
                }

                UnturnedChat.Say(caller, baseClusteringPlugin.Translate("no_action_queued"));
                return;
            }

            if (confirm)
            {
                if (!m_WreckActions.TryGetValue(cId, out var action))
                {
                    UnturnedChat.Say(caller, baseClusteringPlugin.Translate("no_action_queued"));
                    return;
                }

                m_WreckActions.Remove(cId);

                var remove = BuildableDirectory.GetBuildables(includePlants: action.IncludeVehicles);

                if (action.TargetPlayer != null)
                    remove = remove.Where(k => k.Owner.ToString().Equals(action.TargetPlayer.Id));

                if (action.FilterForBarricades) remove = remove.Where(k => k.Asset is ItemBarricadeAsset);
                else if (action.FilterForStructures) remove = remove.Where(k => k.Asset is ItemStructureAsset);

                if (action.ItemAssets.Count > 0)
                    remove = remove.Where(k => action.ItemAssets.Exists(l => k.AssetId == l.id));

                if (!action.Center.IsNegativeInfinity())
                    remove = remove.Where(k =>
                        (k.Position - action.Center).sqrMagnitude <= Mathf.Pow(action.Radius, 2));

                var buildables = remove.ToList();
                if (!buildables.Any())
                {
                    UnturnedChat.Say(caller, baseClusteringPlugin.Translate("cannot_wreck_no_builds"));
                    return;
                }

                foreach (var build in buildables)
                    build.SafeDestroy();

                UnturnedChat.Say(caller,
                    baseClusteringPlugin.Translate("wrecked", buildables.Count, action.ItemAssetName,
                        !float.IsNegativeInfinity(action.Radius)
                            ? action.Radius.ToString(CultureInfo.CurrentCulture)
                            : baseClusteringPlugin.Translate("not_available"),
                        action.TargetPlayer != null
                            ? action.TargetPlayer.DisplayName
                            : baseClusteringPlugin.Translate("not_available"), action.IncludeVehicles,
                        action.FilterForBarricades, action.FilterForStructures));
                return;
            }

            var builds = BuildableDirectory.GetBuildables(includePlants: plants);

            if (target != null) builds = builds.Where(k => k.Owner.ToString().Equals(target.Id));

            if (barricades) builds = builds.Where(k => k.Asset is ItemBarricadeAsset);
            else if (structs) builds = builds.Where(k => k.Asset is ItemStructureAsset);

            if (assetCount > 0) builds = builds.Where(k => itemAssets.Exists(l => k.AssetId == l.id));

            var center = Vector3.negativeInfinity;

            if (!float.IsNegativeInfinity(radius))
            {
                if (caller is not UnturnedPlayer cPlayer)
                {
                    UnturnedChat.Say(caller, baseClusteringPlugin.Translate("cannot_be_executed_from_console"));
                    return;
                }

                center = cPlayer.Position;
                builds = builds.Where(k => (k.Position - center).sqrMagnitude <= Mathf.Pow(radius, 2));
            }

            var itemAssetName = baseClusteringPlugin.Translate("not_available");

            switch (assetCount)
            {
                case 1:
                    itemAssetName = itemAssets.First().itemName;
                    break;
                case > 1:
                    itemAssetName = itemAssetInput;
                    break;
            }

            var count = builds.Count();
            if (count <= 0)
            {
                UnturnedChat.Say(caller, baseClusteringPlugin.Translate("cannot_wreck_no_builds"));
                return;
            }

            if (m_WreckActions.TryGetValue(cId, out _))
            {
                m_WreckActions[cId] = new WreckAction(plants, barricades, structs, target, center, itemAssets, radius,
                    itemAssetName);
                UnturnedChat.Say(caller,
                    baseClusteringPlugin.Translate("wreck_action_queued_new", itemAssetName,
                        baseClusteringPlugin.Translate("not_available"),
                        !float.IsNegativeInfinity(radius)
                            ? radius.ToString(CultureInfo.CurrentCulture)
                            : baseClusteringPlugin.Translate("not_available"),
                        target != null ? target.DisplayName : baseClusteringPlugin.Translate("not_available"),
                        plants, barricades, structs, count));
            }
            else
            {
                m_WreckActions.Add(cId,
                    new WreckAction(plants, barricades, structs, target, center, itemAssets, radius, itemAssetName));
                UnturnedChat.Say(caller,
                    baseClusteringPlugin.Translate("wreck_action_queued", itemAssetName,
                        !float.IsNegativeInfinity(radius)
                            ? radius.ToString(CultureInfo.CurrentCulture)
                            : baseClusteringPlugin.Translate("not_available"),
                        target != null ? target.DisplayName : baseClusteringPlugin.Translate("not_available"),
                        plants, barricades, structs, count));
            }
        }
    }
}