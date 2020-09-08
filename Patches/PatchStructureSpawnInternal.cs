using System;
using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Classes;
using Pustalorc.Plugins.BaseClustering.API.Delegates;
using SDG.Unturned;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.Patches
{
    [HarmonyPatch(typeof(StructureManager), "dropReplicatedStructure")]
    public static class PatchStructureSpawnInternal
    {
        public static event BuildableSpawned OnNewStructureSpawned;

        [HarmonyPrefix]
        public static bool DropReplicatedStructure(Structure structure, Vector3 point, Quaternion rotation, ulong owner,
                // ReSharper disable InconsistentNaming
                // ReSharper disable once RedundantAssignment
                ulong group, ref uint ___instanceCount, ref List<Collider> ___structureColliders, ref bool __result)
            // ReSharper restore InconsistentNaming
        {
            var eulerAngles = rotation.eulerAngles;
            var angle1 = (float) (Mathf.RoundToInt(eulerAngles.x / 2f) * 2);
            var angle2 = (float) (Mathf.RoundToInt(eulerAngles.y / 2f) * 2);
            var angle3 = (float) (Mathf.RoundToInt(eulerAngles.z / 2f) * 2);

            if (!Regions.tryGetCoordinate(point, out var x, out var y) ||
                !StructureManager.tryGetRegion(x, y, out var region))
            {
                __result = false;
                return false;
            }

            var data = new StructureData(structure, point, MeasurementTool.angleToByte(angle1),
                MeasurementTool.angleToByte(angle2), MeasurementTool.angleToByte(angle3), owner, group,
                Provider.time, ++___instanceCount);

            var drop = SpawnStructure(region, structure.id, data.point,
                data.angle_x, data.angle_y, data.angle_z, 100,
                data.owner, data.group, data.instanceID, ref ___structureColliders);

            if (drop != null)
            {
                region.structures.Add(data);
                StructureManager.instance.channel.send("tellStructure", ESteamCall.OTHERS, x, y,
                    StructureManager.STRUCTURE_REGIONS, ESteamPacket.UPDATE_RELIABLE_BUFFER, x, y,
                    structure.id, data.point, data.angle_x,
                    data.angle_y, data.angle_z, data.owner,
                    data.group, data.instanceID);
            }

            OnNewStructureSpawned?.Invoke(new Buildable(data, drop));
            __result = true;
            return false;
        }

        [CanBeNull]
        public static StructureDrop SpawnStructure(StructureRegion region, ushort id, Vector3 point, byte angleX,
            byte angleY, byte angleZ, byte hp, ulong owner, ulong group, uint instanceID,
            ref List<Collider> structureColliders)
        {
            if (id == 0) return null;

            var asset = Assets.find(EAssetType.ITEM, id);
            if (asset == null)
            {
                if (Provider.isServer) return null;

                Assets.reportError($"Missing structure ID {id}, must disconnect");
                Provider.connectionFailureInfo = ESteamConnectionFailureInfo.STRUCTURE;
                Provider.connectionFailureReason = id.ToString();
                Provider.disconnect();

                return null;
            }

            StructureDrop drop = null;
            try
            {
                var itemStructureAsset = asset as ItemStructureAsset;
                var transform = StructureTool.getStructure(id, hp, owner, group, itemStructureAsset);
                transform.parent = LevelStructures.models;
                transform.position = point;
                transform.rotation =
                    Quaternion.Euler(angleX * 2, angleY * 2, angleZ * 2);
                if (!Dedicator.isDedicated && itemStructureAsset != null &&
                    (itemStructureAsset.construct == EConstruct.FLOOR ||
                     itemStructureAsset.construct == EConstruct.FLOOR_POLY))
                    LevelGround.cutFoliage(point, itemStructureAsset.foliageCutRadius);
                drop = new StructureDrop(transform, instanceID);
                region.drops.Add(drop);
                structureColliders.Clear();
                transform.GetComponentsInChildren(structureColliders);

                foreach (var collider in structureColliders)
                {
                    if (collider is MeshCollider) collider.enabled = false;
                    if (collider is MeshCollider) collider.enabled = true;
                }

                if (StructureManager.onStructureSpawned != null) StructureManager.onStructureSpawned(region, drop);
            }
            catch (Exception e)
            {
                UnturnedLog.warn("Exception while spawning structure: {0}", id);
                UnturnedLog.exception(e);
            }

            return drop;
        }
    }
}