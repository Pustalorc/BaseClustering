using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Buildables;
using Pustalorc.Plugins.BaseClustering.API.Utilities;
using Rocket.API;
using Rocket.Unturned.Chat;

#pragma warning disable 1591

namespace Pustalorc.Plugins.BaseClustering.Commands;

[UsedImplicitly]
public sealed class TopBuildersCommand : IRocketCommand
{
    public AllowedCaller AllowedCaller => AllowedCaller.Both;

    public string Name => "topbuilders";

    public string Help => "Displays the top 5 builders in the game.";

    public string Syntax => "v";

    public List<string> Aliases => new() { "topb" };

    public List<string> Permissions => new() { "topbuilders" };

    public void Execute(IRocketPlayer caller, string[] command)
    {
        var args = command.ToList();
        var pluginInstance = BaseClusteringPlugin.Instance;

        if (pluginInstance == null)
            throw new NullReferenceException("BaseClusteringPlugin.Instance is null. Cannot execute command.");


        var plants = args.CheckArgsIncludeString("v", out var index);
        if (index > -1)
            args.RemoveAt(index);

        var builds = BuildableDirectory.GetBuildables(includePlants: plants);

        var topBuilders = builds.GroupBy(k => k.Owner).OrderByDescending(k => k.Count()).Take(5).ToList();

        for (var i = 0; i < topBuilders.Count; i++)
        {
            var builder = topBuilders.ElementAt(i);

            UnturnedChat.Say(caller,
                pluginInstance.Translate("top_builder_format", i + 1, builder.Key, builder.Count()));
        }
    }
}