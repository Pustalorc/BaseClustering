using System.Collections.Generic;
using JetBrains.Annotations;
using Rocket.API;
using Rocket.Unturned.Chat;

namespace Pustalorc.Plugins.BaseClustering.Commands
{
    public sealed class ClustersRegenCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        [NotNull] public string Name => "clustersregen";

        [NotNull] public string Help => "Regenerates all clusters from scratch.";

        [NotNull] public string Syntax => "";

        [NotNull] public List<string> Aliases => new List<string>();

        [NotNull] public List<string> Permissions => new List<string> {"clustersregen"};

        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedChat.Say(caller, BaseClusteringPlugin.Instance.Translate("clusters_regen_warning"));
            BaseClusteringPlugin.Instance.BaseClusterDirectory.GenerateAndLoadAllClusters(false);
        }
    }
}