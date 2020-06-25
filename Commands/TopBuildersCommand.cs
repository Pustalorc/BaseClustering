using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using PlayerInfoLibrary;
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

        public async void Execute(IRocketPlayer caller, [NotNull] string[] command)
        {
            var args = command.ToList();

            var plants = args.CheckArgsIncludeString("v", out var index);
            if (index > -1)
                args.RemoveAt(index);

            var builds = ReadOnlyGame.GetBuilds(CSteamID.Nil, true, plants);

            var topBuilders = builds.GroupBy(k => k.Owner).OrderByDescending(k => k.Count()).Take(5).ToList();

            for (var i = 0; i < topBuilders.Count; i++)
            {
                var builder = topBuilders.ElementAt(i);

                PlayerData pInfo = null;
                if (PlayerInfoLib.Instance != null)
                    pInfo = await PlayerInfoLib.Instance.database?.QueryById(new CSteamID(builder.Key));

                UnturnedChat.Say(caller,
                    BaseClusteringPlugin.Instance.Translate("top_builder_format", i + 1,
                        pInfo?.CharacterName ?? builder.Key.ToString(), builder.Key, builder.Count()));
            }
        }
    }
}