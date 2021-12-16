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
    /// <summary>
    /// Main class for the Base Clustering Plugin. Handles instances of both <see cref="BuildableDirectory"/> and <see cref="BaseClusterDirectory"/>.
    /// </summary>
    public sealed class BaseClusteringPlugin : RocketPlugin<BaseClusteringPluginConfiguration>
    {
        /// <summary>
        /// A singleton accessor for the plugin.
        /// </summary>
        public static BaseClusteringPlugin? Instance { get; private set; }

        /// <summary>
        /// This event is only raised when the plugin has fully loaded.
        /// <br/>
        /// To be exact, the plugin instantiates everything first on <see cref="BaseClusteringPlugin.Load()"/> and then has the instances correctly initialize once the level has loaded completely.
        /// </summary>
        [UsedImplicitly]
        public static event VoidDelegate? OnPluginFullyLoaded;

        /// <summary>
        /// Harmony instance that the plugin utilizes.
        /// </summary>
        private static Harmony? _harmony;

        /// <summary>
        /// The main instance of type <see cref="BuildableDirectory"/>.
        /// </summary>
        public BuildableDirectory? BuildableDirectory { get; private set; }

        /// <summary>
        /// The main instance of type <see cref="BaseClusterDirectory"/>.
        /// </summary>
        public BaseClusterDirectory? BaseClusterDirectory { get; private set; }

        /// <summary>
        /// Gets the default translations that the plugin uses.
        /// </summary>
        public override TranslationList DefaultTranslations => new TranslationList
        {
            {
                "command_fail_clustering_disabled",
                "This command is disabled as the base clustering feature is disabled."
            },
            {
                "clusters_regen_warning",
                "WARNING! This operation can take a long amount of time! The more buildables in the map the longer it will take! Please see console for when this operation is completed."
            },
            { "not_available", "N/A" },
            { "cannot_be_executed_from_console", "That command cannot be executed from console with those arguments." },
            {
                "build_count",
                "There are a total of {0} builds. Specific Item: {1}, Radius: {2}, Player: {3}, Planted Barricades Included: {4}, Filter by Barricades: {5}, Filter by Structures: {6}"
            },
            { "cluster_count", "There are a total of {0} clusters. Specific Item: {1}, Radius: {2}, Player: {3}" },
            {
                "not_looking_buildable", "You are not looking at a structure/barricade, so you cannot get any info."
            },
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
            { "top_builder_format", "At number {0}, {1} with {2} buildables!" },
            { "top_cluster_format", "At number {0}, {1} with {2} clusters!" },
            { "not_enough_args", "You need more arguments to use this command." },
            { "action_cancelled", "The wreck action was cancelled." },
            { "no_action_queued", "There is no wreck action queued." },
            { "cannot_wreck_no_clusters", "There are no clusters selected, so nothing can be wrecked." },
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
            { "cannot_wreck_no_builds", "There are no buildables selected, so nothing can be wrecked." },
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
            { "vehicle_wreck", "Wrecked buildables from {0} [{1}]. Instance ID: {2}, Owner: {3}" }
        };

        /// <summary>
        /// Loads and initializes the plugin.
        /// </summary>
        protected override void Load()
        {
            if (_harmony == null)
            {
                _harmony = new Harmony("com.pustalorc.baseClustering");
                _harmony.PatchAll();
            }

            BuildableDirectory = new BuildableDirectory(Configuration.Instance);

            if (Configuration.Instance.EnableClustering)
                BaseClusterDirectory = new BaseClusterDirectory(this, Configuration.Instance, BuildableDirectory);

            if (Level.isLoaded)
                OnLevelLoaded(0);
            else
                Level.onLevelLoaded += OnLevelLoaded;

            Provider.onCommenceShutdown += SaveManager.save;

            Instance = this;
            Logging.PluginLoaded(this);
        }

        /// <summary>
        /// Unloads and de-initializes the plugin.
        /// </summary>
        protected override void Unload()
        {
            Instance = null;

            Provider.onCommenceShutdown -= SaveManager.save;
            Level.onLevelLoaded -= OnLevelLoaded;

            if (BaseClusterDirectory != null)
            {
                BaseClusterDirectory.Unload();
                BaseClusterDirectory = null;
            }

            if (BuildableDirectory != null)
            {
                BuildableDirectory.Unload();
                BuildableDirectory = null;
            }

            Logging.PluginUnloaded(this);
        }

        private void OnLevelLoaded(int level)
        {
            BuildableDirectory?.LevelLoaded();
            BaseClusterDirectory?.LevelLoaded();
            OnPluginFullyLoaded?.Invoke();
        }
    }
}