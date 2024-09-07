namespace Pustalorc.Libraries.BaseClustering.API.BaseClusters.Directory.Constants;

internal static class LoggingConstants
{
    public const string BuildablesLoaded = "Loaded {0} buildables from the map. Took {1}ms";

    public const string ClustersGeneratingWarning =
        "Generating new clusters. This can take a LONG time. How long will depend on the following factors (but not limited to): CPU usage, CPU cores/threads, Buildables in the map. This generation only needs to be ran once from raw.";

    public const string ClustersLoaded = "Clusters Loaded: {0}. Took {1}ms.";

    public const string BuildableCountMismatch =
        "Warning! Buildable count doesn't match saved count! Buildable save data was most likely modified or lost during server downtime. Clusters will be now rebuilt.";

    public const string BuildableCountMismatchDebug =
        "Buildables according to IBuildableDirectory: {0}. Buildables according to save file: {1}.";

    public const string ClusterLoadProgress = "Loading saved clusters... {0}% [{1}/{2}] {3}ms";
    public const string ClusterSaveProgress = "Saving clusters... {0}% [{1}/{2}] {3}ms";

    public const string BuildNotFoundDuringLoad =
        "Warning! Buildable with InstanceId {0} [isStructure: {1}] not found! Save data was most likely modified or lost during server downtime. Clusters will be now rebuilt.";

    public const string ClusterLoadException =
        "Warning! An exception was thrown when attempting to load the save file. Assuming the data is corrupted. Clusters will be now rebuilt. Exception: {0}";

    public const string ClusterGenerationProgress = "Generating new clusters... {0}% [{1}/{2}] {3}ms";
}