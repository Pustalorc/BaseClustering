using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Rocket.API;
using Rocket.Unturned.Chat;
using Steamworks;

namespace Pustalorc.Plugins.BaseClustering.Commands
{
    public sealed class TopClustersCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        [NotNull] public string Name => "topclusters";

        [NotNull] public string Help => "Displays the top 5 clusters in the game.";

        [NotNull] public string Syntax => "";

        [NotNull] public List<string> Aliases => new List<string> {"topc"};

        [NotNull] public List<string> Permissions => new List<string> {"topclusters"};

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var clusters = BaseClusteringPlugin.Instance.Clusters;

            var topClusters = clusters.GroupBy(k => k.CommonOwner).OrderByDescending(k => k.Count()).Take(5).ToList();

            for (var i = 0; i < topClusters.Count; i++)
            {
                var builder = topClusters.ElementAt(i);
                
                UnturnedChat.Say(caller,
                    BaseClusteringPlugin.Instance.Translate("top_cluster_format", i + 1, builder.Key, builder.Count()));
            }
        }
    }
}