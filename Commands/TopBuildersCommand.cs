using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using PlayerInfoLibrary;
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

        [NotNull] public string Syntax => "[include]";

        [NotNull] public List<string> Aliases => new List<string> {"topb"};

        [NotNull] public List<string> Permissions => new List<string> {"topbuilders"};

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var topBuilders = BaseClusteringPlugin.Instance.Buildables.GroupBy(k => k.Owner).OrderBy(k => k.Count());

            for (var i = 0; i < 5; i++)
            {
                var builder = topBuilders.ElementAt(i);
                var pInfo = PlayerInfoLib.Instance.database.QueryById(new CSteamID(builder.Key));

                UnturnedChat.Say(caller,
                    BaseClusteringPlugin.Instance.Translate("top_builder_format", i + 1,
                        pInfo?.CharacterName ?? builder.Key.ToString(), builder.Key, builder.Count()));
            }
        }
    }
}