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

namespace Pustalorc.Plugins.BaseClustering.Commands
{
    public sealed class WreckCommand : IRocketCommand
    {
        private readonly Dictionary<string, WreckAction> m_WreckActions = new Dictionary<string, WreckAction>();

        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        [NotNull] public string Name => "wreck";

        [NotNull] public string Help => "Destroys buildables from the map.";

        [NotNull]
        public string Syntax =>
            "confirm | abort | b [radius] | s [radius] | <item> [radius] | v [item] [radius] | <player> [item] [radius]";

        [NotNull] public List<string> Aliases => new List<string> {"w"};

        [NotNull] public List<string> Permissions => new List<string> {"wreck"};

        public void Execute([NotNull] IRocketPlayer caller, [NotNull] string[] command)
        {
            var cId = caller.Id;
            var args = command.ToList();

            if (args.Count == 0)
            {
                UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("not_enough_args"));
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

            var itemAssetInput = BaseClusteringPlugin.Instance.Translate("not_available");
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
                    UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("action_cancelled"));
                    return;
                }

                UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("no_action_queued"));
                return;
            }

            if (confirm)
            {
                if (!m_WreckActions.TryGetValue(cId, out var action))
                {
                    UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("no_action_queued"));
                    return;
                }

                m_WreckActions.Remove(cId);

                var remove = BuildableDirectory.GetBuildables(includePlants: action.IncludeVehicles);

                if (action.TargetPlayer != null)
                    remove = remove.Where(k => k.Owner.ToString().Equals(action.TargetPlayer.Id));

                if (action.FilterForBarricades) remove = remove.Where(k => k.Asset is ItemBarricadeAsset);
                else if (action.FilterForStructures) remove = remove.Where(k => k.Asset is ItemStructureAsset);

                if (action.ItemAssets != null) remove = remove.Where(k => action.ItemAssets.Exists(l => k.AssetId == l.id));

                var buildables = remove.ToList();
                if (!buildables.Any())
                {
                    UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("cannot_wreck_no_builds"));
                    return;
                }

                foreach (var build in buildables)
                    build.SafeDestroy();

                UnturnedChat.Say(caller,BaseClusteringPlugin.Instance.Translate("wrecked", buildables.Count, action.ItemAssetName,!float.IsNegativeInfinity(action.Radius)? action.Radius.ToString(CultureInfo.CurrentCulture): BaseClusteringPlugin.Instance.Translate("not_available"),action.TargetPlayer != null? action.TargetPlayer.DisplayName: BaseClusteringPlugin.Instance.Translate("not_available"), action.IncludeVehicles,action.FilterForBarricades, action.FilterForStructures));
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
                if (!(caller is UnturnedPlayer cPlayer))
                {
                    UnturnedChat.Say(caller,
                        BaseClusteringPlugin.Instance.Translate("cannot_be_executed_from_console"));
                    return;
                }

                center = cPlayer.Position;
                builds = builds.Where(k => (k.Position - center).sqrMagnitude <= Mathf.Pow(radius, 2));
            }

            var itemAssetName = BaseClusteringPlugin.Instance.Translate("not_available");
            if (assetCount == 1)
                itemAssetName = itemAssets.First().itemName;
            else if (assetCount > 1)
                itemAssetName = itemAssetInput;

            var count = builds.Count();
            if (count <= 0)
            {
                UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("cannot_wreck_no_builds"));
                return;
            }

            if (m_WreckActions.TryGetValue(cId, out _))
            {
                m_WreckActions[cId] = new WreckAction(plants, barricades, structs, target, center, itemAssets, radius, itemAssetName);
                UnturnedChat.Say(caller,BaseClusteringPlugin.Instance.Translate("wreck_action_queued_new", itemAssetName, BaseClusteringPlugin.Instance.Translate("not_available"),!float.IsNegativeInfinity(radius)? radius.ToString(CultureInfo.CurrentCulture): BaseClusteringPlugin.Instance.Translate("not_available"),target != null ? target.DisplayName : BaseClusteringPlugin.Instance.Translate("not_available"),plants, barricades, structs, count));
            }
            else
            {
                m_WreckActions.Add(cId,new WreckAction(plants, barricades, structs, target, center, itemAssets, radius, itemAssetName));
                UnturnedChat.Say(caller,BaseClusteringPlugin.Instance.Translate("wreck_action_queued", itemAssetName,!float.IsNegativeInfinity(radius)? radius.ToString(CultureInfo.CurrentCulture): BaseClusteringPlugin.Instance.Translate("not_available"),target != null ? target.DisplayName : BaseClusteringPlugin.Instance.Translate("not_available"),plants, barricades, structs, count));
            }
        }
    }
}