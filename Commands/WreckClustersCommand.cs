using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Classes;
using Pustalorc.Plugins.BaseClustering.API.Statics;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using Steamworks;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.Commands
{
    public sealed class WreckClustersCommand : IRocketCommand
    {
        private Dictionary<CSteamID, WreckClustersAction>
            WreckActions = new Dictionary<CSteamID, WreckClustersAction>();

        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        [NotNull] public string Name => "wreckclusters";

        [NotNull] public string Help => "Destroys clusters from the map.";

        [NotNull] public string Syntax => "confirm | abort | [player] [item] [radius]";

        [NotNull] public List<string> Aliases => new List<string> {"wc"};

        [NotNull] public List<string> Permissions => new List<string> {"wreckclusters"};

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

                var remove = action.TargetPlayer != null
                    ? BaseClusteringPlugin.Instance.Clusters.Where(
                        k => k.CommonOwner.ToString().Equals(action.TargetPlayer.Id))
                    : BaseClusteringPlugin.Instance.Clusters;

                if (action.ItemAsset != null)
                    remove = remove.Where(k => k.Buildables.Any(l => l.AssetId == action.ItemAsset.id));

                if (action.Center != Vector3.negativeInfinity)
                    remove = remove.Where(k =>
                        k.Buildables.Any(l => Vector3.Distance(l.Position, action.Center) <= action.Radius));

                var baseClusters = remove.ToList();
                if (!baseClusters.Any())
                {
                    UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("cannot_wreck_no_clusters"));
                    return;
                }

                foreach (var cluster in baseClusters)
                    BaseClusteringPlugin.Instance.DestroyCluster(cluster);

                UnturnedChat.Say(caller,
                    BaseClusteringPlugin.Instance.Translate("wrecked_clusters", baseClusters.Count,
                        action.ItemAsset != null
                            ? action.ItemAsset.itemName
                            : BaseClusteringPlugin.Instance.Translate("not_available"),
                        action.Radius != float.NegativeInfinity
                            ? action.Radius.ToString(CultureInfo.CurrentCulture)
                            : BaseClusteringPlugin.Instance.Translate("not_available"),
                        action.TargetPlayer != null
                            ? action.TargetPlayer.DisplayName
                            : BaseClusteringPlugin.Instance.Translate("not_available")));
                return;
            }

            var clusters = target != null
                ? BaseClusteringPlugin.Instance.Clusters.Where(k => k.CommonOwner.ToString().Equals(target.Id))
                : BaseClusteringPlugin.Instance.Clusters;

            if (itemAsset != null) clusters = clusters.Where(k => k.Buildables.Any(l => l.AssetId == itemAsset.id));

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
                clusters = clusters.Where(k => k.Buildables.Any(l => Vector3.Distance(l.Position, center) <= radius));
            }

            if (!clusters.Any())
            {
                UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("cannot_wreck_no_clusters"));
                return;
            }

            WreckActions.Add(cId, new WreckClustersAction(target, center, itemAsset, radius));
        }
    }
}