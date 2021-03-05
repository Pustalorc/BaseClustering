using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Utilities;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.Commands
{
    public sealed class FindClustersCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        [NotNull] public string Name => "findclusters";
        [NotNull] public string Help => "Finds clusters around the map";
        [NotNull] public string Syntax => "<player> [id] [radius] | [id] [radius]";
        [NotNull] public List<string> Aliases => new List<string> {"fc"};
        [NotNull] public List<string> Permissions => new List<string> {"findclusters"};

        public void Execute(IRocketPlayer caller, [NotNull] string[] command)
        {
            var args = command.ToList();

            var target = args.GetIRocketPlayer(out var index);
            if (index > -1)
                args.RemoveAt(index);

            var itemAsset = args.GetItemAsset(out index);
            if (index > -1)
                args.RemoveAt(index);

            var radius = args.GetFloat(out index);
            if (index > -1)
                args.RemoveAt(index);

            var clusters = target == null
                ? BaseClusteringPlugin.Instance.Clusters
                : BaseClusteringPlugin.Instance.Clusters.Where(k =>
                    k.Buildables.Any(l => l.Owner.ToString().Equals(target.Id)));

            if (itemAsset != null) clusters = clusters.Where(k => k.Buildables.Any(l => l.AssetId == itemAsset.id));

            if (!float.IsNegativeInfinity(radius))
            {
                if (!(caller is UnturnedPlayer cPlayer))
                {
                    UnturnedChat.Say(caller,
                        BaseClusteringPlugin.Instance.Translate("cannot_be_executed_from_console"));
                    return;
                }

                clusters = clusters.Where(k =>
                    k.Buildables.Any(l => (l.Position - cPlayer.Position).sqrMagnitude <= Mathf.Pow(radius, 2)));
            }

            UnturnedChat.Say(caller,
                BaseClusteringPlugin.Instance.Translate("cluster_count", clusters.Count(),
                    itemAsset != null ? itemAsset.itemName : BaseClusteringPlugin.Instance.Translate("not_available"),
                    !float.IsNegativeInfinity(radius)
                        ? radius.ToString(CultureInfo.CurrentCulture)
                        : BaseClusteringPlugin.Instance.Translate("not_available"),
                    target != null ? target.DisplayName : BaseClusteringPlugin.Instance.Translate("not_available")));
        }
    }
}