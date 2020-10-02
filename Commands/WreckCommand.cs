using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Classes;
using Pustalorc.Plugins.BaseClustering.API.Statics;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.Commands
{
    public sealed class WreckCommand : IRocketCommand
    {
        private Dictionary<string, WreckAction> m_WreckActions = new Dictionary<string, WreckAction>();

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

            var itemAsset = args.GetItemAsset(out index);
            if (index > -1)
                args.RemoveAt(index);

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

                var remove = ReadOnlyGame.GetBuilds(includePlants: action.IncludeVehicles);

                if (action.TargetPlayer != null)
                    remove = remove.Where(k => k.Owner.ToString().Equals(action.TargetPlayer.Id));

                if (action.FilterForBarricades) remove = remove.Where(k => k.Asset is ItemBarricadeAsset);
                else if (action.FilterForStructures) remove = remove.Where(k => k.Asset is ItemStructureAsset);

                if (action.ItemAsset != null) remove = remove.Where(k => k.AssetId == action.ItemAsset.id);

                if (!action.Center.IsNegativeInfinity())
                    remove = remove.Where(k => Vector3.Distance(k.Position, action.Center) <= action.Radius);

                var buildables = remove.ToList();
                if (!buildables.Any())
                {
                    UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("cannot_wreck_no_builds"));
                    return;
                }

                foreach (var build in buildables)
                    WriteOnlyGame.RemoveBarricadeStructure(build.Model);

                UnturnedChat.Say(caller,
                    BaseClusteringPlugin.Instance.Translate("wrecked", buildables.Count,
                        action.ItemAsset != null
                            ? action.ItemAsset.itemName
                            : BaseClusteringPlugin.Instance.Translate("not_available"),
                        action.Radius != float.NegativeInfinity
                            ? action.Radius.ToString(CultureInfo.CurrentCulture)
                            : BaseClusteringPlugin.Instance.Translate("not_available"),
                        action.TargetPlayer != null
                            ? action.TargetPlayer.DisplayName
                            : BaseClusteringPlugin.Instance.Translate("not_available"), action.IncludeVehicles,
                        action.FilterForBarricades, action.FilterForStructures));
                return;
            }

            var builds = ReadOnlyGame.GetBuilds(includePlants: plants);

            if (target != null) builds = builds.Where(k => k.Owner.ToString().Equals(target.Id));

            if (barricades) builds = builds.Where(k => k.Asset is ItemBarricadeAsset);
            else if (structs) builds = builds.Where(k => k.Asset is ItemStructureAsset);

            if (itemAsset != null) builds = builds.Where(k => k.AssetId == itemAsset.id);

            var center = Vector3.negativeInfinity;

            if (radius != float.NegativeInfinity)
            {
                if (!(caller is UnturnedPlayer cPlayer))
                {
                    UnturnedChat.Say(caller,
                        BaseClusteringPlugin.Instance.Translate("cannot_be_executed_from_console"));
                    return;
                }

                center = cPlayer.Position;
                builds = builds.Where(k => Vector3.Distance(k.Position, center) <= radius);
            }

            if (!builds.Any())
            {
                UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("cannot_wreck_no_builds"));
                return;
            }

            if (m_WreckActions.TryGetValue(cId, out _))
            {
                m_WreckActions[cId] = new WreckAction(plants, barricades, structs, target, center, itemAsset, radius);
                UnturnedChat.Say(caller,
                    BaseClusteringPlugin.Instance.Translate("wreck_action_queued_new",
                        itemAsset != null
                            ? itemAsset.itemName
                            : BaseClusteringPlugin.Instance.Translate("not_available"),
                        radius != float.NegativeInfinity
                            ? radius.ToString(CultureInfo.CurrentCulture)
                            : BaseClusteringPlugin.Instance.Translate("not_available"),
                        target != null ? target.DisplayName : BaseClusteringPlugin.Instance.Translate("not_available"),
                        plants, barricades, structs));
            }
            else
            {
                m_WreckActions.Add(cId,
                    new WreckAction(plants, barricades, structs, target, center, itemAsset, radius));
                UnturnedChat.Say(caller,
                    BaseClusteringPlugin.Instance.Translate("wreck_action_queued",
                        itemAsset != null
                            ? itemAsset.itemName
                            : BaseClusteringPlugin.Instance.Translate("not_available"),
                        radius != float.NegativeInfinity
                            ? radius.ToString(CultureInfo.CurrentCulture)
                            : BaseClusteringPlugin.Instance.Translate("not_available"),
                        target != null ? target.DisplayName : BaseClusteringPlugin.Instance.Translate("not_available"),
                        plants, barricades, structs));
            }
        }
    }
}