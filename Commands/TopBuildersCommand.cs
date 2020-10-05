using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Statics;
using Rocket.API;
using Rocket.Unturned.Chat;
using Steamworks;

namespace Pustalorc.Plugins.BaseClustering.Commands
{
    public sealed class TopBuildersCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        [NotNull] public string Name => "topbuilders";

        [NotNull] public string Help => "Displays the top 5 builders in the game.";

        [NotNull] public string Syntax => "v";

        [NotNull] public List<string> Aliases => new List<string> {"topb"};

        [NotNull] public List<string> Permissions => new List<string> {"topbuilders"};

        public void Execute(IRocketPlayer caller, [NotNull] string[] command)
        {
            var args = command.ToList();

            var plants = args.CheckArgsIncludeString("v", out var index);
            if (index > -1)
                args.RemoveAt(index);

            var builds = ReadOnlyGame.GetBuilds(includePlants: plants);

            var topBuilders = builds.GroupBy(k => k.Owner).OrderByDescending(k => k.Count()).Take(5).ToList();

            for (var i = 0; i < topBuilders.Count; i++)
            {
                var builder = topBuilders.ElementAt(i);

                UnturnedChat.Say(caller,
                    BaseClusteringPlugin.Instance.Translate("top_builder_format", i + 1, builder.Key, builder.Count()));
            }
        }
    }
}