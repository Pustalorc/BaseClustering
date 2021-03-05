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

namespace Pustalorc.Plugins.BaseClustering.Commands
{
    public sealed class FindBuildsCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        [NotNull] public string Name => "findbuilds";
        [NotNull] public string Help => "Finds buildables around the map";

        [NotNull]
        public string Syntax =>
            "b [radius] | s [radius] | [id] [radius] | v [id] [radius] | [player] [id] [radius] | [player] b [radius] | [player] s [radius] | [player] v [id] [radius]";

        [NotNull] public List<string> Aliases => new List<string> {"fb"};
        [NotNull] public List<string> Permissions => new List<string> {"findbuilds"};

        public void Execute(IRocketPlayer caller, [NotNull] string[] command)
        {
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

            var itemAsset = args.GetItemAsset(out index);
            if (index > -1)
                args.RemoveAt(index);

            var radius = args.GetFloat(out index);
            if (index > -1)
                args.RemoveAt(index);

            var builds = BuildableCollection.GetBuildables(includePlants: plants);

            if (target != null) builds = builds.Where(k => k.Owner.ToString().Equals(target.Id));

            if (barricades) builds = builds.Where(k => k.Asset is ItemBarricadeAsset);
            else if (structs) builds = builds.Where(k => k.Asset is ItemStructureAsset);

            if (itemAsset != null) builds = builds.Where(k => k.AssetId == itemAsset.id);

            if (!float.IsNegativeInfinity(radius))
            {
                if (!(caller is UnturnedPlayer cPlayer))
                {
                    UnturnedChat.Say(caller,
                        BaseClusteringPlugin.Instance.Translate("cannot_be_executed_from_console"));
                    return;
                }

                builds = builds.Where(k => (k.Position - cPlayer.Position).sqrMagnitude <= Mathf.Pow(radius, 2));
            }

            UnturnedChat.Say(caller,
                BaseClusteringPlugin.Instance.Translate("build_count", builds.Count(),
                    itemAsset != null ? itemAsset.itemName : BaseClusteringPlugin.Instance.Translate("not_available"),
                    !float.IsNegativeInfinity(radius)
                        ? radius.ToString(CultureInfo.CurrentCulture)
                        : BaseClusteringPlugin.Instance.Translate("not_available"),
                    target != null ? target.DisplayName : BaseClusteringPlugin.Instance.Translate("not_available"),
                    plants, barricades, structs));
        }
    }
}