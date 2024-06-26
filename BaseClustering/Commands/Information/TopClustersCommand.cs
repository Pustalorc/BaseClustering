using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Directory.Interfaces;
using Pustalorc.Libraries.RocketModCommandsExtended.Abstractions;
using Pustalorc.Libraries.RocketModServices.Services;
using Pustalorc.Plugins.BaseClustering.Commands.Constants;
using Rocket.API;

namespace Pustalorc.Plugins.BaseClustering.Commands.Information;

internal sealed class TopClustersCommand(Dictionary<string, string> translations)
    : RocketCommandWithTranslations(true, translations)
{
    public override AllowedCaller AllowedCaller => AllowedCaller.Both;

    public override string Name => "topClusters";

    public override string Help => "Displays the top 5 clusters in the game.";

    public override string Syntax => "";

    public override List<string> Aliases => ["topC"];

    public override Dictionary<string, string> DefaultTranslations => new()
    {
        { TranslationKeys.CommandExceptionKey, CommandTranslationConstants.CommandExceptionValue },
        { CommandTranslationConstants.TopClusterFormatKey, CommandTranslationConstants.TopClusterFormatValue }
    };

    public override Task ExecuteAsync(IRocketPlayer caller, string[] command)
    {
        var clusterDirectory = RocketModService<IBaseClusterDirectory>.GetService();
        var clusters = clusterDirectory.Clusters;

        var topClusters = clusters.GroupBy(static k => k.Owner).OrderByDescending(static k => k.Count()).Take(5)
            .ToList();

        for (var i = 0; i < topClusters.Count; i++)
        {
            var builder = topClusters.ElementAt(i);

            SendTranslatedMessage(caller, CommandTranslationConstants.TopClusterFormatKey, i + 1, builder.Key,
                builder.Count());
        }

        return Task.CompletedTask;
    }
}