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
    [HarmonyPatch(typeof(BarricadeManager), "dropBarricadeIntoRegionInternal")]
    public static class PatchBarricadeSpawnInternal
    {
        public static event BuildableSpawned OnNewBarricadeSpawned;

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
            OnNewBarricadeSpawned?.Invoke(new Buildable(data, drop));
            return false;
        }

        [CanBeNull]
        public static BarricadeDrop SpawnBarricade(BarricadeRegion region, ushort id, byte[] state, Vector3 point,
            byte angleX, byte angleY, byte angleZ, byte hp, ulong owner, ulong group, uint instanceID,
            ref List<Collider> barricadeColliders)
        {
            if (id == 0) return null;

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
                var transform = BarricadeTool.getBarricade(region.parent, hp, owner, group, point,
                    Quaternion.Euler(angleX * 2, angleY * 2, angleZ * 2), id, state,
                    itemBarricadeAsset);
                if (region.parent != LevelBarricades.models)
                {
                    barricadeColliders.Clear();
                    transform.GetComponentsInChildren(barricadeColliders);
                    foreach (var collider in barricadeColliders)
                    {
                        var flag = collider is MeshCollider;
                        if (flag) collider.enabled = false;
                        if (collider.GetComponent<Rigidbody>() == null)
                        {
                            var rigidbody = collider.gameObject.AddComponent<Rigidbody>();
                            rigidbody.useGravity = false;
                            rigidbody.isKinematic = true;
                        }

                        if (flag) collider.enabled = true;
                        if (collider.gameObject.layer == 27) collider.gameObject.layer = 14;
                    }

                    transform.gameObject.SetActive(false);
                    transform.gameObject.SetActive(true);
                    var component = region.parent.GetComponent<InteractableVehicle>();
                    if (component != null) component.ignoreCollisionWith(barricadeColliders, true);
                }

                drop = new BarricadeDrop(transform, transform.GetComponent<Interactable>(), instanceID,
                    itemBarricadeAsset);
                region.drops.Add(drop);
                if (BarricadeManager.onBarricadeSpawned != null) BarricadeManager.onBarricadeSpawned(region, drop);
            }
            catch (Exception e)
            {
                UnturnedLog.warn("Exception while spawning barricade: {0}", id);
                UnturnedLog.exception(e);
            }

            return drop;
        }
    }
}