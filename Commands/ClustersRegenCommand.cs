using System;
using System.Collections.Generic;
using Rocket.API;
using Rocket.Unturned.Chat;

namespace Pustalorc.Plugins.BaseClustering.Commands
{
    public sealed class ClustersRegenCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public string Name => "clustersregen";

        public string Help => "Regenerates all clusters from scratch.";

        public string Syntax => "";

        public List<string> Aliases => new();

        public List<string> Permissions => new() {"clustersregen"};

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var pluginInstance = BaseClusteringPlugin.Instance;

            if (pluginInstance == null)
                throw new NullReferenceException("BaseClusteringPlugin.Instance is null. Cannot execute command.");

            var clusterDirectory = pluginInstance.BaseClusterDirectory;
            if (clusterDirectory == null)
            {
                UnturnedChat.Say(caller, pluginInstance.Translate("command_fail_clustering_disabled"));
                return;
            }

            UnturnedChat.Say(caller, pluginInstance.Translate("clusters_regen_warning"));
            clusterDirectory.GenerateAndLoadAllClusters(false);
        }
    }
}