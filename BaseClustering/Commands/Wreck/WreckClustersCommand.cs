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

namespace Pustalorc.Plugins.BaseClustering.Commands.Wreck;

internal sealed class WreckClustersCommand(Dictionary<string, string> translations)
    : RocketCommandWithTranslations(true, translations)
{
    public override AllowedCaller AllowedCaller => AllowedCaller.Both;

    public override string Name => "wreckClusters";

    public override string Help => "Destroys clusters from the map.";

    public override string Syntax => "confirm | abort | [player] [item] [radius]";

    public override List<string> Aliases => ["wc"];

    public override Dictionary<string, string> DefaultTranslations => new()
    {
        { TranslationKeys.CommandExceptionKey, CommandTranslationConstants.CommandExceptionValue },
        { CommandTranslationConstants.NotEnoughArgumentsKey, CommandTranslationConstants.NotEnoughArgumentsValue },
        { CommandTranslationConstants.NotAvailableKey, CommandTranslationConstants.NotAvailableValue }
    };

    private Dictionary<string, WreckClustersAction> WreckActions { get; } = new();

    public override async Task ExecuteAsync(IRocketPlayer caller, string[] command)
    {
        var clusterDirectory = RocketModService<IBaseClusterDirectory>.GetService();
        var args = command.ToList();

        if (args.Count == 0)
        {
            SendTranslatedMessage(caller, CommandTranslationConstants.NotEnoughArgumentsKey);
            return;
        }

        if (args.CheckArgsIncludeString("abort", out var index))
        {
            await Cancel(caller);
            return;
        }

        if (args.CheckArgsIncludeString("confirm", out index))
        {
            await Confirm(caller);
            return;
        }

        var target = args.GetIRocketPlayer(out index);
        if (index > -1)
            args.RemoveAt(index);

        var notAvailable = Translate(CommandTranslationConstants.NotAvailableKey);
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

        if (target != null && ulong.TryParse(target.Id, out var tId))
            clusters = clusters.Where(cluster => cluster.Owner == tId);

        if (assetCount > 0)
            clusters = clusters.Where(k => k.Buildables.Any(l => itemAssets.Exists(z => l.AssetId == z.id)));

        var center = Vector3.negativeInfinity;

        if (!float.IsNegativeInfinity(radius))
        {
            if (caller is not UnturnedPlayer cPlayer)
            {
                SendTranslatedMessage(caller, "cannot_be_executed_from_console");
                return;
            }

            center = cPlayer.Position;
            clusters = clusters.Where(k =>
                k.Buildables.Any(l => (l.Position - center).sqrMagnitude <= Mathf.Pow(radius, 2)));
        }

        var count = clusters.Count();

        if (count <= 0)
        {
            SendTranslatedMessage(caller, "cannot_wreck_no_clusters");
            return;
        }

        var itemAssetName = assetCount switch
        {
            1 => itemAssets.First().itemName,
            > 1 => itemAssetInput,
            _ => notAvailable
        };

        var callerId = caller.Id;
        if (WreckActions.TryGetValue(callerId, out _))
        {
            WreckActions[callerId] = new WreckClustersAction(target, center, itemAssets, radius, itemAssetInput);
            SendTranslatedMessage(caller, "wreck_clusters_action_queued_new",
                target?.DisplayName ?? Translate("not_available"), itemAssetName,
                !float.IsNegativeInfinity(radius)
                    ? radius.ToString(CultureInfo.CurrentCulture)
                    : Translate("not_available"), count);
        }
        else
        {
            WreckActions.Add(callerId, new WreckClustersAction(target, center, itemAssets, radius, itemAssetInput));
            SendTranslatedMessage(caller, "wreck_clusters_action_queued",
                target?.DisplayName ?? Translate("not_available"), itemAssetName,
                !float.IsNegativeInfinity(radius)
                    ? radius.ToString(CultureInfo.CurrentCulture)
                    : Translate("not_available"), count);
        }
    }

    private Task Cancel(IRocketPlayer caller)
    {
        if (WreckActions.Remove(caller.Id))
        {
            SendTranslatedMessage(caller, CommandTranslationConstants.ActionCancelledKey);
            return Task.CompletedTask;
        }

        SendTranslatedMessage(caller, CommandTranslationConstants.NoActionQueuedKey);
        return Task.CompletedTask;
    }

    private Task Confirm(IRocketPlayer caller)
    {
        var callerId = caller.Id;
        if (!WreckActions.TryGetValue(callerId, out var action))
        {
            SendTranslatedMessage(caller, CommandTranslationConstants.NoActionQueuedKey);
            return Task.CompletedTask;
        }

        WreckActions.Remove(callerId);
        var clusterDirectory = RocketModService<IBaseClusterDirectory>.GetService();
        var notAvailable = Translate(CommandTranslationConstants.NotAvailableKey);
        var targetName = notAvailable;
        var radiusText = notAvailable;

        var clusters = clusterDirectory.Clusters.AsEnumerable();

        if (action.TargetPlayer != null && ulong.TryParse(action.TargetPlayer.Id, out var targetId))
        {
            targetName = action.TargetPlayer.DisplayName;
            clusters = clusters.Where(cluster => cluster.Owner == targetId);
        }

        if (!float.IsNegativeInfinity(action.Radius))
            radiusText = action.Radius.ToString(CultureInfo.CurrentCulture);

        if (action.ItemAssets.Count > 0)
            clusters = clusters.Where(k => k.Buildables.Any(l => action.ItemAssets.Exists(z => l.AssetId == z.id)));

        if (!action.Center.IsNegativeInfinity())
            clusters = clusters.Where(k =>
                k.Buildables.Any(l =>
                    (l.Position - action.Center).sqrMagnitude <= Mathf.Pow(action.Radius, 2)));

        var clusterList = clusters.ToList();
        if (!clusterList.Any())
        {
            SendTranslatedMessage(caller, CommandTranslationConstants.CannotWreckNoClustersKey);
            return Task.CompletedTask;
        }

        foreach (var cluster in clusterList)
            cluster.Destroy();

        SendTranslatedMessage(caller, CommandTranslationConstants.WreckedClustersKey, clusterList.Count,
            action.ItemAssetName, radiusText, targetName);
        return Task.CompletedTask;
    }
}