using System.Collections.Generic;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Directory.Implementations;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Directory.Interfaces;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Pool.Implementations;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Pool.Interfaces;
using Pustalorc.Libraries.Logging.Manager;
using Pustalorc.Libraries.RocketModCommandsExtended.Abstractions;
using Pustalorc.Libraries.RocketModCommandsExtended.Extensions;
using Pustalorc.Libraries.RocketModServices.Services;
using Pustalorc.Plugins.BaseClustering.Commands.Actions;
using Pustalorc.Plugins.BaseClustering.Commands.Information;
using Pustalorc.Plugins.BaseClustering.Commands.Wreck;
using Pustalorc.Plugins.BaseClustering.Config;
using Pustalorc.Plugins.BaseClustering.Constants;
using Rocket.Core.Plugins;
using SDG.Unturned;

namespace Pustalorc.Plugins.BaseClustering;

/// <inheritdoc />
public sealed class BaseClusteringPlugin : RocketPlugin<BaseClusteringPluginConfiguration>
{
    private List<MultiThreadedRocketCommand> Commands { get; }

    /// <inheritdoc />
    public BaseClusteringPlugin()
    {
        var translations = this.GetCurrentTranslationsForCommands();

        Commands = new List<MultiThreadedRocketCommand>
        {
            new ClustersRegenCommand(translations),
            new TeleportToClusterCommand(translations),
            new FindClustersCommand(translations),
            new TopClustersCommand(translations),
            new WreckClustersCommand(translations)
        };

        Commands.LoadAndRegisterCommands(this);
    }

    /// <inheritdoc />
    protected override void Load()
    {
        if (Level.isLoaded)
            OnLevelLoaded(0);
        else
            Level.onPrePreLevelLoaded += OnLevelLoaded;

        Provider.onCommenceShutdown += SaveManager.save;
        Commands.ReloadCommands(this);

        LogManager.UpdateConfiguration(Configuration.Instance);
        LogManager.Information(LoggingConstants.PluginLoaded);
    }

    /// <inheritdoc />
    protected override void Unload()
    {
        Provider.onCommenceShutdown -= SaveManager.save;
        Level.onPrePreLevelLoaded -= OnLevelLoaded;

        RocketModService<IBaseClusterDirectory>.UnregisterService();
        RocketModService<IBaseClusterPool>.UnregisterService();

        LogManager.Information(LoggingConstants.PluginUnloaded);
    }

    private void OnLevelLoaded(int level)
    {
        LogManager.Debug(LoggingConstants.LevelLoaded);
        if (RocketModService<IBaseClusterPool>.TryGetService() == null)
        {
            LogManager.Debug(LoggingConstants.NoIBaseClusterPoolService);
            RocketModService<IBaseClusterPool>.RegisterService(new DefaultBaseClusterPool());
            LogManager.Information(LoggingConstants.DefaultBaseClusterPoolRegistered);
        }

        LogManager.Debug(LoggingConstants.UpdateIBaseClusterPoolConfig);
        RocketModService<IBaseClusterPool>.GetService().ChangeClusterRules(Configuration.Instance);

        LogManager.Debug(LoggingConstants.CheckIBaseClusterDirectoryService);
        if (RocketModService<IBaseClusterDirectory>.TryGetService() == null)
        {
            LogManager.Debug(LoggingConstants.NoIBaseClusterDirectoryService);
            RocketModService<IBaseClusterDirectory>.RegisterService(new DefaultBaseClusterDirectory());
            LogManager.Information(LoggingConstants.DefaultBaseClusterDirectoryRegistered);
        }

        LogManager.Debug(LoggingConstants.UpdateIBaseClusterDirectoryConfig);
        RocketModService<IBaseClusterDirectory>.GetService().ChangeClusterRules(Configuration.Instance);

        LogManager.Information(LoggingConstants.LoadingFinished);
    }
}