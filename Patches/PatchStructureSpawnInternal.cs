using System;
using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Delegates;
using SDG.Unturned;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.Patches
{
    [HarmonyPatch(typeof(StructureManager), "dropBarricadeIntoRegionInternal")]
    public static class PatchStructureSpawnInternal
    {
        public static event StructureSpawned OnNewStructureSpawned;

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

            using (new StructureRegionSyncTest(region, nameof(StructureManager.dropReplicatedStructure)))
            {
                var structureData = new StructureData(structure, point, MeasurementTool.angleToByte(angle1),
                    MeasurementTool.angleToByte(angle2), MeasurementTool.angleToByte(angle3), owner, group,
                    Provider.time, ++___instanceCount);

                var model = SpawnStructure(region, structure.id, structureData.point,
                    structureData.angle_x, structureData.angle_y, structureData.angle_z, 100,
                    structureData.owner, structureData.group, structureData.instanceID, ref ___structureColliders);

                if (model != null)
                {
                    region.structures.Add(structureData);
                    StructureManager.instance.channel.send("tellStructure", ESteamCall.OTHERS, x, y,
                        StructureManager.STRUCTURE_REGIONS, ESteamPacket.UPDATE_RELIABLE_BUFFER, (object) x, (object) y,
                        (object) structure.id, (object) structureData.point, (object) structureData.angle_x,
                        (object) structureData.angle_y, (object) structureData.angle_z, (object) structureData.owner,
                        (object) structureData.group, (object) structureData.instanceID);
                }

                OnNewStructureSpawned?.Invoke(structureData, model);
                __result = true;
                return false;
            }
        }

        [CanBeNull]
        public static StructureDrop SpawnStructure(StructureRegion region, ushort id, Vector3 point, byte angleX,
            byte angleY,
            byte angleZ, byte hp, ulong owner, ulong group, uint instanceId, ref List<Collider> colliders)
        {
            if (id == 0)
                return null;

            var asset1 = Assets.find(EAssetType.ITEM, id);
            if (asset1 == null)
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
                var asset2 = asset1 as ItemStructureAsset;
                var newModel = StructureTool.getStructure(id, hp, owner, group, asset2);
                newModel.parent = LevelStructures.models;
                newModel.position = point;
                newModel.rotation = Quaternion.Euler(angleX * 2, angleY * 2,
                    angleZ * 2);
                if (!Dedicator.isDedicated && asset2 != null &&
                    (asset2.construct == EConstruct.FLOOR || asset2.construct == EConstruct.FLOOR_POLY))
                    LevelGround.cutFoliage(point, asset2.foliageCutRadius);
                drop = new StructureDrop(newModel, instanceId);
                region.drops.Add(drop);
                colliders.Clear();
                newModel.GetComponentsInChildren(colliders);
                foreach (var collider in colliders)
                {
                    if (collider is MeshCollider)
                        collider.enabled = false;
                    if (collider is MeshCollider)
                        collider.enabled = true;
                }

                if (StructureManager.onStructureSpawned != null)
                    StructureManager.onStructureSpawned(region, drop);
            }
            catch (Exception ex)
            {
                UnturnedLog.warn("Exception while spawning structure: {0}", (object) id);
                UnturnedLog.exception(ex);
            }

            return drop;
        }
    }
}