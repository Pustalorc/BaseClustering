using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Rocket.API;
using Rocket.Unturned.Chat;

#pragma warning disable 1591

namespace Pustalorc.Plugins.BaseClustering.Commands
{
    [UsedImplicitly]
    public sealed class TopClustersCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public string Name => "topclusters";

        public string Help => "Displays the top 5 clusters in the game.";

        public string Syntax => "";

        public List<string> Aliases => new List<string> { "topc" };

        public List<string> Permissions => new List<string> { "topclusters" };

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

            var clusters = clusterDirectory.Clusters;

            var topClusters = clusters.GroupBy(k => k.CommonOwner).OrderByDescending(k => k.Count()).Take(5).ToList();

            for (var i = 0; i < topClusters.Count; i++)
            {
                var builder = topClusters.ElementAt(i);

                UnturnedChat.Say(caller,
                    pluginInstance.Translate("top_cluster_format", i + 1, builder.Key, builder.Count()));
            }
        }
    }
}