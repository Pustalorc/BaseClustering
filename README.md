# BaseClustering

Unturned Plugin to cluster Buildables &amp; Structures

Download is available on github [releases](https://github.com/Pustalorc/BaseClustering/releases/)

---

## Commands

`/clustersregen` - Regenerates ALL clusters. Useful if a new barricade was missed, or one of them is reported at the
wrong position, or some other issue with clusters has occurred.

`/findbuilds [b | s | v] [player] [item] [radius]` - Finds and returns the count of buildables with the filters.

`/findclusters [player] [item] [radius]` - Finds and returns the count of clusters with the filters.

`/removebuildable` - Removes the buildable that the player is currently looking at.

`/teleporttobuild [b | s | v] [player] [item]` - Teleports to a random buildable that satisfies the filters.

`/teleporttocluster [player]` - Teleports to a random cluster that satisfies the filters.

`/topbuilders [v]` - Lists the top 5 players that have the most amount of buildables on the map.

`/topclusters` - Lists the top 5 players that have the most amount of common ownerships on clusters.

`/wreckclusters [player] [item] [radius]` and `/wreckclusters [abort | confirm]` - Wrecks all of the clusters that
satisfy the filters. Requires confirmation before fully wrecking them.

`/wreck [b | s | v] [player] [item] [radius]` and `/wreck [abort | confirm]` - Wrecks all of the buildables that satisfy
the filters. Requires confirmation before fully wrecking them.

`/wreckvehicle` - Wrecks all of the buildables without confirmation on the vehicle that you are facing.

### Explanation of arguments:

Arguments can be on any order, so doing: `/wreck b pusta birch 5.0` should be the same as `/wreck birch b 5.0 pusta`

`[b | s | v]` specifies filters for all of the buildables. It can specify to filter JUST for barricades (`b`), JUST for
structures (`s`), or INCLUDE buildables on vehicles (`v`).

`[player]` self-explanatory. Accepts Steam64ID as well as the name.

`[item]` self-explanatory. Accepts item names (including just typing `birch`) as well as item IDs. Using item names will
select all results with that name, so be careful if you only write one letter!

`[radius]` self-explanatory. Note that typing `5` will be considered an item if you do not specify an item by name or ID
before it! To prevent this, type `.0` at the end of the number, that will force it to be detected as a radius.

`[abort | confirm]` - self-explanatory, aborts or confirms previous action

---

## Default Configuration:

```xml
<?xml version="1.0" encoding="utf-8"?>
<BaseClusteringPluginConfiguration xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <EnableClustering>true</EnableClustering> <!-- Enables/Disables the clustering feature from the plugin. Setting this to false means you only really want to use the plugin as an API for other plugins, or as another wreckingball plugin. -->
  <MaxDistanceBetweenStructures>6.1</MaxDistanceBetweenStructures> <!-- Maximum distance between structures in order for them to be considered part of a base. Structures are floors, walls, roofs, pillars, etc. -->
  <MaxDistanceToConsiderPartOfBase>10</MaxDistanceToConsiderPartOfBase> <!-- Maximum distance between barricades (and barricade <-> structure) in order for them to be considered part of a base. Barricades are doors, signs, crates, beds, flags, etc. This distance is also used for any checks if anything is inside the base. -->
  <BuildableCapacity>60000</BuildableCapacity> <!-- The starting size of the dictionaries that the plugin internally uses. Larger value will result in higher memory usage, but no need to allocate more space when more elements are added. Lower is the opposite, less memory usage, but requires to allocate more space when more elements are added. -->
  <BackgroundWorkerSleepTime>125</BackgroundWorkerSleepTime> <!-- How long the background working that's deferring the destroyed/spawned buildables should wait before processing another batch of deferred buildables. -->
</BaseClusteringPluginConfiguration>
```

---

## Default Translations:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Translations xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <Translation Id="command_fail_clustering_disabled" Value="This command is disabled as the base clustering feature is disabled." />
  <Translation Id="clusters_regen_warning" Value="WARNING! This operation can take a long amount of time! The more buildables in the map the longer it will take! Please see console for when this operation is completed." />
  <Translation Id="not_available" Value="N/A" />
  <Translation Id="cannot_be_executed_from_console" Value="That command cannot be executed from console with those arguments." />
  <Translation Id="build_count" Value="There are a total of {0} builds. Specific Item: {1}, Radius: {2}, Player: {3}, Planted Barricades Included: {4}, Filter by Barricades: {5}, Filter by Structures: {6}" />
  <Translation Id="cluster_count" Value="There are a total of {0} clusters. Specific Item: {1}, Radius: {2}, Player: {3}" />
  <Translation Id="not_looking_buildable" Value="You are not looking at a structure/barricade, so you cannot get any info." />
  <Translation Id="cannot_teleport_no_builds" Value="Cannot teleport anywhere, no buildables found with the following filters. Specific Item: {0}, Player: {1}, Planted Barricades Included: {2}, Filter by Barricades: {3}, Filter by Structures: {4}" />
  <Translation Id="cannot_teleport_builds_too_close" Value="Cannot teleport anywhere, all buildables with the specified filters are too close. Specific Item: {0}, Player: {1}, Planted Barricades Included: {2}, Filter by Barricades: {3}, Filter by Structures: {4}" />
  <Translation Id="cannot_teleport_no_clusters" Value="Cannot teleport anywhere, no clusters found with the following filters. Player: {0}" />
  <Translation Id="top_builder_format" Value="At number {0}, {1} with {2} buildables!" />
  <Translation Id="top_cluster_format" Value="At number {0}, {1} with {2} clusters!" />
  <Translation Id="not_enough_args" Value="You need more arguments to use this command." />
  <Translation Id="action_cancelled" Value="The wreck action was cancelled." />
  <Translation Id="no_action_queued" Value="There is no wreck action queued." />
  <Translation Id="cannot_wreck_no_clusters" Value="There are no clusters selected, so nothing can be wrecked." />
  <Translation Id="wrecked_clusters" Value="Wrecked {0} clusters. Specific Item: {1}, Radius: {2}, Player: {3}" />
  <Translation Id="wreck_clusters_action_queued" Value="Queued a wreck clusters action for {3} clusters. Confirm with /wc confirm. Player: {0}, Specific Item: {1}, Radius: {2}." />
  <Translation Id="wreck_clusters_action_queued_new" Value="Discarded previous queued action and queued a new wreck clusters action for {3} clusters. Confirm with /wc confirm. Player: {0}, Specific Item: {1}, Radius: {2}." />
  <Translation Id="cannot_wreck_no_builds" Value="There are no buildables selected, so nothing can be wrecked." />
  <Translation Id="wrecked" Value="Wrecked {0} buildables. Specific Item: {1}, Radius: {2}, Player: {3}, Planted Barricades Included: {4}, Filter by Barricades: {5}, Filter by Structures: {6}" />
  <Translation Id="wreck_action_queued" Value="Queued a wreck action for {6} buildables. Confirm with /w confirm. Specific Item: {0}, Radius: {1}, Player: {2}, Planted Barricades Included: {3}, Filter by Barricades: {4}, Filter by Structures: {5}" />
  <Translation Id="wreck_action_queued_new" Value="Discarded previous queued action and queued a new wreck action for {6} buildables. Confirm with /w confirm. Specific Item: {0}, Radius: {1}, Player: {2}, Planted Barricades Included: {3}, Filter by Barricades: {4}, Filter by Structures: {5}" />
  <Translation Id="no_vehicle_found" Value="Couldn't find a vehicle in the direction you're looking, or you are too far away from one. Maximum distance is 10 units." />
  <Translation Id="vehicle_dead" Value="The vehicle you are looking at is destroyed and cannot be wrecked. Please look at a vehicle that isn't destroyed." />
  <Translation Id="vehicle_no_plant" Value="The vehicle appears to have no assigned barricades to it, please make sure that it has barricades before asking to wreck them." />
  <Translation Id="vehicle_wreck" Value="Wrecked buildables from {0} [{1}]. Instance ID: {2}, Owner: {3}" />
</Translations>
```