using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Directory.Interfaces;
using Pustalorc.Libraries.RocketModCommandsExtended.Abstractions;
using Pustalorc.Libraries.RocketModServices.Services;
using Pustalorc.Plugins.BaseClustering.Commands.Constants;
using Pustalorc.Plugins.BaseClustering.Commands.Extensions;
using Rocket.API;
using Rocket.Unturned.Player;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.Commands.Information;

internal sealed class FindClustersCommand(Dictionary<string, string> translations)
    : RocketCommandWithTranslations(true, translations)
{
    public override AllowedCaller AllowedCaller => AllowedCaller.Both;
    public override string Name => "findClusters";
    public override string Help => "Finds clusters around the map";
    public override string Syntax => "<player> [id] [radius] | [id] [radius]";
    public override List<string> Aliases => ["fc"];

    public override Dictionary<string, string> DefaultTranslations => new()
    {
        { TranslationKeys.CommandExceptionKey, CommandTranslationConstants.CommandExceptionValue },
        { CommandTranslationConstants.NotAvailableKey, CommandTranslationConstants.NotAvailableValue },
        {
            CommandTranslationConstants.CannotBeExecutedFromConsoleKey,
            CommandTranslationConstants.CannotBeExecutedFromConsoleValue
        },
        { CommandTranslationConstants.ClusterCountKey, CommandTranslationConstants.ClusterCountValue }
    };

    public override Task ExecuteAsync(IRocketPlayer caller, string[] command)
    {
        var clusterDirectory = RocketModService<IBaseClusterDirectory>.GetService();
        var args = command.ToList();
        var notAvailable = Translate(CommandTranslationConstants.NotAvailableKey);

        var target = args.GetIRocketPlayer(out var index);
        if (index > -1)
            args.RemoveAt(index);

        var itemAssetInput = notAvailable;
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

        var clusters = clusterDirectory.Clusters.AsEnumerable();
        var targetName = notAvailable;

        if (target != null && ulong.TryParse(target.Id, out var targetId))
        {
            targetName = target.DisplayName;
            clusters = clusters.Where(cluster => cluster.Owner == targetId);
        }

        if (assetCount > 0)
            clusters = clusters.Where(k => k.Buildables.Any(l => itemAssets.Exists(z => l.AssetId == z.id)));

        var radiusText = notAvailable;

        if (!float.IsNegativeInfinity(radius))
        {
            if (caller is not UnturnedPlayer cPlayer)
            {
                SendTranslatedMessage(caller, CommandTranslationConstants.CannotBeExecutedFromConsoleKey);
                return Task.CompletedTask;
            }

            radiusText = radius.ToString(CultureInfo.CurrentCulture);
            clusters = clusters.Where(k =>
                k.Buildables.Any(l => (l.Position - cPlayer.Position).sqrMagnitude <= Mathf.Pow(radius, 2)));
        }

        var itemAssetName = assetCount switch
        {
            1 => itemAssets.First().itemName,
            > 1 => itemAssetInput,
            _ => notAvailable
        };

        SendTranslatedMessage(caller, CommandTranslationConstants.ClusterCountKey, clusters.Count(), itemAssetName,
            radiusText, targetName);
        return Task.CompletedTask;
    }
}