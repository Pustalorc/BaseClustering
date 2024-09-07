namespace Pustalorc.Plugins.BaseClustering.Commands.Constants;

internal static class CommandTranslationConstants
{
    public const string CommandExceptionValue =
        "An issue occurred during command execution of /{0} {1}. Error message: {2}. Stack trace: {3}";

    public const string ClustersRegenWarningKey = "clusters_regen_warning";

    public const string ClustersRegenWarningValue =
        "WARNING! This operation can take a long amount of time! The more buildables in the map the longer it will take! Please see console for when this operation is completed.";

    public const string NotAvailableKey = "not_available";
    public const string NotAvailableValue = "N/A";
    public const string CannotTeleportNoClustersKey = "cannot_teleport_no_clusters";

    public const string CannotTeleportNoClustersValue =
        "Cannot teleport anywhere, no clusters found with the following filters. Player: {0}";

    public const string NotEnoughArgumentsKey = "not_enough_args";
    public const string NotEnoughArgumentsValue = "You need more arguments to use this command.";
    public const string CannotBeExecutedFromConsoleKey = "cannot_be_executed_from_console";

    public const string CannotBeExecutedFromConsoleValue =
        "That command cannot be executed from console with those arguments.";

    public const string ClusterCountKey = "cluster_count";

    public const string ClusterCountValue =
        "There are a total of {0} clusters. Specific Item: {1}, Radius: {2}, Player: {3}";

    public const string TopClusterFormatKey = "top_cluster_format";
    public const string TopClusterFormatValue = "At number {0}, {1} with {2} clusters!";
    public const string ActionCancelledKey = "action_cancelled";
    public const string ActionCancelledValue = "The wreck action was cancelled.";
    public const string NoActionQueuedKey = "no_action_queued";
    public const string NoActionQueuedValue = "There is no wreck action queued.";
    public const string CannotWreckNoClustersKey = "cannot_wreck_no_clusters";
    public const string CannotWreckNoClustersValue = "There are no clusters selected, so nothing can be wrecked.";
    public const string WreckedClustersKey = "wrecked_clusters";
    public const string WreckedClustersValue = "Wrecked {0} clusters. Specific Item: {1}, Radius: {2}, Player: {3}";
    public const string WreckedClustersActionQueuedKey = "wreck_clusters_action_queued";

    public const string WreckedClustersActionQueuedValue =
        "Queued a wreck clusters action for {3} clusters. Confirm with /wc confirm. Player: {0}, Specific Item: {1}, Radius: {2}.";

    public const string WreckedClustersActionQueuedNewKey = "wreck_clusters_action_queued_new";

    public const string WreckedClustersActionQueuedNewValue =
        "Discarded previous queued action and queued a new wreck clusters action for {3} clusters. Confirm with /wc confirm. Player: {0}, Specific Item: {1}, Radius: {2}.";
}