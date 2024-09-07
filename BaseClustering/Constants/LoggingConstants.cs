namespace Pustalorc.Plugins.BaseClustering.Constants;

internal static class LoggingConstants
{
    public const string PluginLoaded =
        "Plugin has a deferred load. Please wait for the level to load and then confirm that the plugin loaded. Created by Pustalorc.";

    public const string PluginUnloaded = "Plugin has been unloaded. Created by Pustalorc.";

    public const string LevelLoaded = "Level has loaded, checking for an existing 'IBaseClusterPool' service...";

    public const string NoIBaseClusterPoolService =
        "No existing 'IBaseClusterPool' service found. Registering 'DefaultBaseClusterPool'...";

    public const string DefaultBaseClusterPoolRegistered =
        "Service 'DefaultBaseClusterPool' registered in 'IBaseClusterPool' successfully!";

    public const string UpdateIBaseClusterPoolConfig =
        "Updating registered 'IBaseClusterPool' service with the latest configuration...";

    public const string CheckIBaseClusterDirectoryService =
        "Checking for an existing 'IBaseClusterDirectory' service...";

    public const string NoIBaseClusterDirectoryService =
        "No existing 'IBaseClusterDirectory' service found. Registering and loading 'DefaultBaseClusterDirectory'...";

    public const string DefaultBaseClusterDirectoryRegistered =
        "Service 'DefaultBaseClusterDirectory' registered in 'IBaseClusterDirectory' and loaded successfully!";

    public const string UpdateIBaseClusterDirectoryConfig =
        "Updating registered 'IBaseClusterDirectory' service with the latest configuration...";

    public const string LoadingFinished = "Plugin finished loading.";
}