using Rocket.API;
using Rocket.Unturned.Chat;
using System.Collections.Generic;
using JetBrains.Annotations;

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
            UnturnedChat.Say(caller,
                "WARNING! This operation can take a heavy amount of time! The more buildables and the bigger the auto-radius increment, the longer it will take! Please see console for when it is done.");
            BaseClusteringPlugin.Instance.GenerateAndLoadAllClusters();
        }
    }
}