# BaseClustering
Unturned Plugin to cluster Buildables &amp; Structures

WARNING: This plugin uses a modified version of PIL that does NOT WORK WITH OLD DATA AND VERSIONS of PIL

Download is available on the [Releases](https://github.com/Pustalorc/BaseClustering/releases/)

---

### Commands

`/clustersregen` - Regenerates ALL clusters. Useful if a new barricade was missed, or one of them is reported at the wrong position

`/findbuilds [b | s | v] [player] [item] [radius]` - Finds and returns the count of buildables with the filters.

`/findclusters [player] [item] [radius]` - Finds and returns the count of clusters with the filters.

`/teleporttobuild [b | s | v] [player] [item]` - Teleports to a random buildable that satisfies the filters.

`/teleporttocluster [player]` - Teleports to a random cluster that satisfies the filters.

`/topbuilders [v]` - Lists the top 5 players that have the most amount of buildables on the map.

`/topclusters` - Lists the top 5 players that have the most amount of common ownerships on clusters.

`/wreckclusters [player] [item] [radius]` and `/wreckclusters [abort | confirm]` - Wrecks all of the clusters that satisfy the filters. Requires confirmation before fully wrecking them.

`/wreck [b | s | v] [player] [item] [radius]` and `/wreck [abort | confirm]` - Wrecks all of the buildables that satisfy the filters. Requires confirmation before fully wrecking them.

`/wreckvehicle` - Wrecks all of the buildables without confirmation on the vehicle that you are facing.

### Explanation of arguments:

Arguments can be on any order, so doing: `/wreck b pusta birch 5.0` should be the same as `/wreck birch b 5.0 pusta`

`[b | s | v]` specifies filters for all of the buildables. It can specify to filter JUST for barricades (`b`), JUST for structures (`s`), or INCLUDE buildables on vehicles (`v`).

`[player]` self-explanatory. Accepts Steam64ID as well as the name.

`[item]` self-explanatory. Accepts item names (including just typing `birch`) as well as item IDs

`[radius]` self-explanatory. Note that typing `5` MIGHT be considered an item if you do not specify an item at all! to prevent this, type `.0` at the end of the number, that will force it to be detected as a radius

`[abort | confirm]` - self-explanatory, aborts or confirms previous action
