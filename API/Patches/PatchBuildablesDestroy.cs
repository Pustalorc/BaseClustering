using HarmonyLib;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Delegates;
using SDG.Unturned;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.API.Patches
{
    /// <summary>
    /// A patch for barricades and structures being destroyed.
    /// </summary>
    public static class PatchBuildablesDestroy
    {
        /// <summary>
        /// Event is fired whenever a barricade or structure is destroyed.
        /// </summary>
        public static event BuildableDeleted? OnBuildableDestroyed;

        [HarmonyPatch]
        internal static class InternalPatches
        {
            [HarmonyPatch(typeof(BarricadeManager), "destroyBarricade", typeof(BarricadeDrop), typeof(byte),
                typeof(byte), typeof(ushort))]
            [HarmonyPrefix]
            [UsedImplicitly]
            internal static void DestroyBarricade(BarricadeDrop barricade)
            {
                ThreadUtil.assertIsGameThread();
                OnBuildableDestroyed?.Invoke(barricade.instanceID, false);
            }

            [HarmonyPatch(typeof(StructureManager), "destroyStructure", typeof(StructureDrop), typeof(byte),
                typeof(byte), typeof(Vector3))]
            [HarmonyPrefix]
            [UsedImplicitly]
            internal static void DestroyStructure(StructureDrop structure)
            {
                ThreadUtil.assertIsGameThread();
                OnBuildableDestroyed?.Invoke(structure.instanceID, true);
            }
        }
    }
}