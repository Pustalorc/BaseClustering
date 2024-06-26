using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Directory.Implementations;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Directory.Interfaces;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Pool.Implementations;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Pool.Interfaces;
using Pustalorc.Libraries.Logging.API.Manager;
using Pustalorc.Libraries.RocketModServices.Services;
using Pustalorc.Plugins.BaseClustering.Config;
using Rocket.Core.Plugins;
using SDG.Unturned;

namespace Pustalorc.Plugins.BaseClustering;

/// <inheritdoc />
public sealed class BaseClusteringPlugin : RocketPlugin<BaseClusteringPluginConfiguration>
{
    /// <inheritdoc />
    protected override void Load()
    {
        if (Level.isLoaded)
            OnLevelLoaded(0);
        else
            Level.onLevelLoaded += OnLevelLoaded;

        Provider.onCommenceShutdown += SaveManager.save;

        LogManager.UpdateConfiguration(Configuration.Instance);
        LogManager.Information(
            "Plugin has a deferred load. Please wait for the level to load and then confirm that the plugin loaded. Created by Pustalorc.");
    }

    /// <inheritdoc />
    protected override void Unload()
    {
        Provider.onCommenceShutdown -= SaveManager.save;
        Level.onLevelLoaded -= OnLevelLoaded;

        RocketModService<IBaseClusterDirectory>.UnregisterService();

        RocketModService<IBaseClusterPool>.UnregisterService();

        LogManager.Information("Plugin has been unloaded. Created by Pustalorc.");
    }

    private void OnLevelLoaded(int level)
    {
        LogManager.Debug("Level has loaded, checking for an existing IBaseClusterPool service...");
        if (RocketModService<IBaseClusterPool>.TryGetService() == null)
        {
            LogManager.Debug("No existing IBaseClusterPool service found. Registering 'DefaultBaseClusterPool'...");
            RocketModService<IBaseClusterPool>.RegisterService(new DefaultBaseClusterPool());
            LogManager.Information("'DefaultBaseClusterPool' registered successfully!");
        }

        LogManager.Debug("Updating registered IBaseClusterPool service with the latest configuration...");
        RocketModService<IBaseClusterPool>.GetService().ChangeClusterRules(Configuration.Instance);

        LogManager.Debug("Checking for an existing IBaseClusterDirectory service...");
        if (RocketModService<IBaseClusterDirectory>.TryGetService() == null)
        {
            LogManager.Debug(
                "No existing IBaseClusterDirectory service found. Registering and loading 'DefaultBaseClusterDirectory'...");
            RocketModService<IBaseClusterDirectory>.RegisterService(new DefaultBaseClusterDirectory());
            LogManager.Information("'DefaultBaseClusterDirectory' registered and loaded successfully!");
        }

        LogManager.Debug("Updating registered IBaseClusterDirectory service with the latest configuration...");
        RocketModService<IBaseClusterDirectory>.GetService().ChangeClusterRules(Configuration.Instance);

        LogManager.Information("Finished loading everything.");
    }
}