using System.Collections.Generic;
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
        private Dictionary<CSteamID, WreckAction> WreckActions = new Dictionary<CSteamID, WreckAction>();

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
            var cId = new CSteamID(ulong.Parse(caller.Id));
            var args = command.ToList();

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
                if (WreckActions.TryGetValue(cId, out _))
                {
                    WreckActions.Remove(cId);
                    UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("action_cancelled"));
                    return;
                }

                UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("no_action_queued"));
                return;
            }

            if (confirm)
            {
                if (!WreckActions.TryGetValue(cId, out var action))
                {
                    UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("no_action_queued"));
                    return;
                }

                WreckActions.Remove(cId);

                var remove = action.IncludeVehicles
                    ? ReadOnlyGame.GetBuilds(CSteamID.Nil, true)
                    : BaseClusteringPlugin.Instance.Buildables;

                if (action.TargetPlayer != null)
                    remove = remove.Where(k => k.Owner.ToString().Equals(action.TargetPlayer.Id));

                if (action.FilterForBarricades) remove = remove.Where(k => k.Asset is ItemBarricadeAsset);
                else if (action.FilterForStructures) remove = remove.Where(k => k.Asset is ItemStructureAsset);

                if (action.ItemAsset != null) remove = remove.Where(k => k.AssetId == action.ItemAsset.id);

                if (action.Center != Vector3.negativeInfinity)
                    remove = remove.Where(k => Vector3.Distance(k.Position, action.Center) <= action.Radius);

                var buildables = remove.ToList();
                if (!buildables.Any())
                {
                    UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("cannot_wreck_no_builds"));
                    return;
                }

                foreach (var build in buildables)
                    WriteOnlyGame.RemoveBarricadeStructure(build.Position);

                UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("wrecked", buildables.Count, action.ItemAsset != null ? action.ItemAsset.itemName : BaseClusteringPlugin.Instance.Translate("not_available"), action.Radius != float.NegativeInfinity ? action.Radius.ToString() : BaseClusteringPlugin.Instance.Translate("not_available"), action.TargetPlayer != null ? action.TargetPlayer.DisplayName : BaseClusteringPlugin.Instance.Translate("not_available"), action.IncludeVehicles, action.FilterForBarricades, action.FilterForStructures));
                return;
            }

            var builds = plants ? ReadOnlyGame.GetBuilds(CSteamID.Nil, true) : BaseClusteringPlugin.Instance.Buildables;

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

            WreckActions.Add(cId, new WreckAction(plants, barricades, structs, target, center, itemAsset, radius));
        }
    }
}