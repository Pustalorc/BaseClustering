using System.Collections.Generic;
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

namespace Pustalorc.Plugins.BaseClustering.Commands.Actions;

internal sealed class TeleportToClusterCommand(Dictionary<string, string> translations)
    : RocketCommandWithTranslations(true, translations)
{
    public override AllowedCaller AllowedCaller => AllowedCaller.Player;

    public override string Name => "teleportToCluster";

    public override string Help => "Teleports you to a random cluster on the map based on filters.";

    public override string Syntax => "[player]";

    public override List<string> Aliases => ["tpc"];

    public override Dictionary<string, string> DefaultTranslations => new()
    {
        { TranslationKeys.CommandExceptionKey, CommandTranslationConstants.CommandExceptionValue },
        { CommandTranslationConstants.NotAvailableKey, CommandTranslationConstants.NotAvailableValue },
        {
            CommandTranslationConstants.CannotTeleportNoClustersKey,
            CommandTranslationConstants.CannotTeleportNoClustersValue
        }
    };

    public override Task ExecuteAsync(IRocketPlayer caller, string[] command)
    {
        if (caller is not UnturnedPlayer player)
            return Task.CompletedTask;

        var clusterDirectory = RocketModService<IBaseClusterDirectory>.GetService();
        var args = command.ToList();
        var targetName = Translate(CommandTranslationConstants.NotAvailableKey);

        var target = args.GetIRocketPlayer(out var index);
        if (index > -1)
            args.RemoveAt(index);

        var clusters = clusterDirectory.Clusters.AsEnumerable();

        if (target != null && ulong.TryParse(target.Id, out var targetId))
        {
            targetName = target.DisplayName;
            clusters = clusters.Where(cluster => cluster.Owner == targetId);
        }

        var clusterList = clusters.Where(static k => k.Center != Vector3.zero).ToList();
        if (!clusterList.Any())
        {
            SendTranslatedMessage(caller, CommandTranslationConstants.CannotTeleportNoClustersKey, targetName);
            return Task.CompletedTask;
        }

        var cluster = clusterList[Random.Range(0, clusterList.Count - 1)];

        if (cluster == null)
        {
            SendTranslatedMessage(caller, CommandTranslationConstants.CannotTeleportNoClustersKey, targetName);
            return Task.CompletedTask;
        }

        var offset = new Vector3(0, 4, 0);

        while (!player.Player.stance.wouldHaveHeightClearanceAtPosition(cluster.Center + offset,
                   0.5f))
            offset.y++;

        player.Teleport(cluster.Center + offset, player.Rotation);
        return Task.CompletedTask;
    }
}