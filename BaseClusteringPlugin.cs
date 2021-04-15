using HarmonyLib;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.BaseClusters;
using Pustalorc.Plugins.BaseClustering.API.Buildables;
using Pustalorc.Plugins.BaseClustering.API.Delegates;
using Pustalorc.Plugins.BaseClustering.API.Utilities;
using Pustalorc.Plugins.BaseClustering.Config;
using Rocket.API.Collections;
using Rocket.Core.Plugins;
using SDG.Unturned;

namespace Pustalorc.Plugins.BaseClustering
{
    public sealed class BaseClusteringPlugin : RocketPlugin<BaseClusteringPluginConfiguration>
    {
        public static BaseClusteringPlugin Instance { get; private set; }
        [UsedImplicitly] public static event VoidDelegate OnPluginFullyLoaded;

        private Harmony m_Harmony;

        public BuildableDirectory BuildableDirectory { get; private set; }
        public BaseClusterDirectory BaseClusterDirectory { get; private set; }

        [NotNull]
        public override TranslationList DefaultTranslations => new TranslationList
        {
            {
                "command_fail_clustering_disabled",
                "This command is disabled as the base clustering feature is disabled."
            },
            {
                "clusters_regen_warning",
                "WARNING! This operation can take a long amount of time! The more buildables in the map the longer it will take! Please see console for when it is done."
            },
            {"cannot_be_executed_from_console", "That command cannot be executed from console with those arguments."},
            {"not_available", "N/A"},
            {
                "build_count",
                "There are a total of {0} builds. Specific Item: {1}, Radius: {2}, Player: {3}, Planted Barricades Included: {4}, Filter by Barricades: {5}, Filter by Structures: {6}"
            },
            {"cluster_count", "There are a total of {0} clusters. Specific Item: {1}, Radius: {2}, Player: {3}"},
            {
                "cannot_teleport_no_builds",
                "Cannot teleport anywhere, no buildables found with the following filters. Specific Item: {0}, Player: {1}, Planted Barricades Included: {2}, Filter by Barricades: {3}, Filter by Structures: {4}"
            },
            {
                "cannot_teleport_builds_too_close",
                "Cannot teleport anywhere, all buildables with the specified filters are too close. Specific Item: {0}, Player: {1}, Planted Barricades Included: {2}, Filter by Barricades: {3}, Filter by Structures: {4}"
            },
            {
                "cannot_teleport_no_clusters",
                "Cannot teleport anywhere, no clusters found with the following filters. Player: {0}"
            },
            {"top_builder_format", "At number {0}, {1} with {2} buildables!"},
            {"top_cluster_format", "At number {0}, {1} with {2} clusters!"},
            {"action_cancelled", "The wreck action was cancelled."},
            {"no_action_queued", "There is no wreck action queued."},
            {"cannot_wreck_no_clusters", "There are no clusters selected, so nothing can be wrecked."},
            {
                "wrecked_clusters",
                "Wrecked {0} clusters. Specific Item: {1}, Radius: {2}, Player: {3}"
            },
            {
                "wreck_clusters_action_queued",
                "Queued a wreck clusters action for {3} clusters. Confirm with /wc confirm. Player: {0}, Specific Item: {1}, Radius: {2}."
            },
            {
                "wreck_clusters_action_queued_new",
                "Discarded previous queued action and queued a new wreck clusters action for {3} clusters. Confirm with /wc confirm. Player: {0}, Specific Item: {1}, Radius: {2}."
            },
            {"cannot_wreck_no_builds", "There are no buildables selected, so nothing can be wrecked."},
            {
                "wrecked",
                "Wrecked {0} buildables. Specific Item: {1}, Radius: {2}, Player: {3}, Planted Barricades Included: {4}, Filter by Barricades: {5}, Filter by Structures: {6}"
            },
            {
                "wreck_action_queued",
                "Queued a wreck action for {6} buildables. Confirm with /w confirm. Specific Item: {0}, Radius: {1}, Player: {2}, Planted Barricades Included: {3}, Filter by Barricades: {4}, Filter by Structures: {5}"
            },
            {
                "wreck_action_queued_new",
                "Discarded previous queued action and queued a new wreck action for {6} buildables. Confirm with /w confirm. Specific Item: {0}, Radius: {1}, Player: {2}, Planted Barricades Included: {3}, Filter by Barricades: {4}, Filter by Structures: {5}"
            },
            {
                "no_vehicle_found",
                "Couldn't find a vehicle in the direction you're looking, or you are too far away from one. Maximum distance is 10 units."
            },
            {
                "vehicle_dead",
                "The vehicle you are looking at is destroyed and cannot be wrecked. Please look at a vehicle that isn't destroyed."
            },
            {
                "vehicle_no_plant",
                "The vehicle appears to have no assigned barricades to it, please make sure that it has barricades before asking to wreck them."
            },
            {"vehicle_wreck", "Wrecked buildables from {0} [{1}]. Instance ID: {2}, Owner: {3}"},
            {"not_enough_args", "You need more arguments to use this command."}
        };

        protected override void Load()
        {
            Instance = this;
            m_Harmony = new Harmony("xyz.pustalorc.baseClustering");
            m_Harmony.PatchAll();

            if (Level.isLoaded)
                OnLevelLoaded(0);
            else
                Level.onLevelLoaded += OnLevelLoaded;

            Logging.PluginLoaded(this);
        }

        protected override void Unload()
        {
            Provider.onCommenceShutdown -= ForceDataSave;

            if (BaseClusterDirectory != null)
            {
                BaseClusterDirectory.Unload();
                BaseClusterDirectory = null;
            }

            BuildableDirectory.Unload();
            BuildableDirectory = null;
            m_Harmony.UnpatchAll();
            m_Harmony = null;
            Instance = null;

            Logging.PluginUnloaded(this);
        }

        private void OnLevelLoaded(int level)
        {
            Provider.onCommenceShutdown += ForceDataSave;
            BuildableDirectory = new BuildableDirectory();

            if (Configuration.Instance.EnableClustering)
                BaseClusterDirectory = new BaseClusterDirectory(Configuration.Instance, BuildableDirectory);
            
            OnPluginFullyLoaded?.Invoke();
        }

        private static void ForceDataSave()
        {
            SaveManager.save();
        }
    }
}