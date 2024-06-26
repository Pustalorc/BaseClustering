using System.Collections.Generic;
using System.Threading.Tasks;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Directory.Interfaces;
using Pustalorc.Libraries.RocketModCommandsExtended.Abstractions;
using Pustalorc.Libraries.RocketModServices.Services;
using Pustalorc.Plugins.BaseClustering.Commands.Constants;
using Rocket.API;

namespace Pustalorc.Plugins.BaseClustering.Commands.Actions;

internal sealed class ClustersRegenCommand(Dictionary<string, string> translations)
    : RocketCommandWithTranslations(false, translations)
{
    public override AllowedCaller AllowedCaller => AllowedCaller.Both;

    public override string Name => "clustersRegen";

    public override string Help => "Regenerates all clusters from scratch.";

    public override string Syntax => "";

    public override Dictionary<string, string> DefaultTranslations => new()
    {
        { TranslationKeys.CommandExceptionKey, CommandTranslationConstants.CommandExceptionValue },
        { CommandTranslationConstants.ClustersRegenWarningKey, CommandTranslationConstants.ClustersRegenWarningValue }
    };

    public override Task ExecuteAsync(IRocketPlayer caller, string[] command)
    {
        var clusterDirectory = RocketModService<IBaseClusterDirectory>.GetService();
        SendTranslatedMessage(caller, CommandTranslationConstants.ClustersRegenWarningKey);
        clusterDirectory.RegenerateClusters();
        return Task.CompletedTask;
    }
}