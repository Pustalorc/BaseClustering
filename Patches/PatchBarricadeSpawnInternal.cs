using System;
using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Delegates;
using SDG.Unturned;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.Patches
{
    [HarmonyPatch(typeof(BarricadeManager), "dropBarricadeIntoRegionInternal")]
    public static class PatchBarricadeSpawnInternal
    {
        public static event BarricadeSpawned OnNewBarricadeSpawned;

        [HarmonyPrefix]
        public static bool DropBarricadeIntoRegionInternal(BarricadeRegion region, [NotNull] Barricade barricade,
                Vector3 point,
                Quaternion rotation, ulong owner, ulong group, [NotNull] out BarricadeData data, out Transform result,
                // ReSharper disable InconsistentNaming
                out uint instanceID, ref uint ___instanceCount, ref List<Collider> ___barricadeColliders)
            // ReSharper restore InconsistentNaming
        {
            result = null;
            var eulerAngles = rotation.eulerAngles;
            var angle1 = (float) (Mathf.RoundToInt(eulerAngles.x / 2f) * 2);
            var angle2 = (float) (Mathf.RoundToInt(eulerAngles.y / 2f) * 2);
            var angle3 = (float) (Mathf.RoundToInt(eulerAngles.z / 2f) * 2);
            instanceID = ++___instanceCount;
            data = new BarricadeData(barricade, point, MeasurementTool.angleToByte(angle1),
                MeasurementTool.angleToByte(angle2), MeasurementTool.angleToByte(angle3), owner, group, Provider.time,
                instanceID);
            var drop = SpawnBarricade(region, barricade.id, barricade.state, data.point, data.angle_x, data.angle_y,
                data.angle_z, 100, data.owner, data.group, instanceID, ref ___barricadeColliders);
            if (drop == null)
                return false;

            region.barricades.Add(data);
            result = drop.model;
            OnNewBarricadeSpawned?.Invoke(data, drop);
            return false;
        }

        [CanBeNull]
        public static BarricadeDrop SpawnBarricade(BarricadeRegion region, ushort id, byte[] state, Vector3 point,
            byte angleX,
            byte angleY, byte angleZ, byte hp, ulong owner, ulong group, uint instanceId,
            ref List<Collider> barricadeColliders)
        {
            if (id == 0)
                return null;

            var asset = Assets.find(EAssetType.ITEM, id);
            if (asset == null)
            {
                if (Provider.isServer) return null;

                Assets.reportError($"Missing barricade ID {id}, must disconnect");
                Provider.connectionFailureInfo = ESteamConnectionFailureInfo.BARRICADE;
                Provider.connectionFailureReason = id.ToString();
                Provider.disconnect();

                return null;
            }

            BarricadeDrop drop = null;
            try
            {
                var itemBarricadeAsset = asset as ItemBarricadeAsset;
                var newModel = BarricadeTool.getBarricade(region.parent, hp, owner, group, point,
                    Quaternion.Euler(angleX * 2, angleY * 2,
                        angleZ * 2),
                    id, state, itemBarricadeAsset);
                barricadeColliders.Clear();
                newModel.GetComponentsInChildren(barricadeColliders);
                if (region.parent != LevelBarricades.models)
                    foreach (var barricadeCollider in barricadeColliders)
                    {
                        if (barricadeCollider is MeshCollider)
                            barricadeCollider.enabled = false;
                        if (barricadeCollider
                            .GetComponent<Rigidbody>() == null)
                        {
                            var rigidBody = barricadeCollider.gameObject
                                .AddComponent<Rigidbody>();
                            rigidBody.useGravity = false;
                            rigidBody.isKinematic = true;
                        }

                        if (barricadeCollider is MeshCollider)
                            barricadeCollider.enabled = true;
                    }

                if (region.parent != LevelBarricades.models)
                {
                    newModel.gameObject.SetActive(false);
                    newModel.gameObject.SetActive(true);

                    var vehicleColliders = new List<Collider>();
                    region.parent.GetComponents(vehicleColliders);
                    RecursivelyAddChildAndBlockColliders(region.parent, ref vehicleColliders);
                    foreach (var barricadeCollider in barricadeColliders)
                    {
                        if (barricadeCollider.gameObject.layer == 27)
                            barricadeCollider.gameObject.layer = 14;
                        foreach (var collider in vehicleColliders)
                            Physics.IgnoreCollision(collider,
                                barricadeCollider, true);
                    }
                }

                drop = new BarricadeDrop(newModel, newModel.GetComponent<Interactable>(), instanceId,
                    itemBarricadeAsset);
                region.drops.Add(drop);
                if (BarricadeManager.onBarricadeSpawned != null)
                    BarricadeManager.onBarricadeSpawned(region, drop);
            }
            catch (Exception ex)
            {
                UnturnedLog.warn("Exception while spawning barricade: {0}", (object) id);
                UnturnedLog.exception(ex);
            }

            return drop;
        }

        public static void RecursivelyAddChildAndBlockColliders([NotNull] Transform parent,
            ref List<Collider> vehicleColliders)
        {
            for (var index = 0; index < parent.childCount; ++index)
            {
                var child = parent.GetChild(index);
                if (child == null) continue;

                if (child.name == "Clip" || child.name == "Block")
                {
                    var subColliders = new List<Collider>();
                    child.GetComponents(subColliders);
                    vehicleColliders.AddRange(subColliders);
                }

                RecursivelyAddChildAndBlockColliders(child, ref vehicleColliders);
            }
        }
    }
}