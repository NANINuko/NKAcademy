using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace NANINuko
{
    [HarmonyPatch]
    internal static class Patch_MapSubset_Load
    {
        static MethodBase TargetMethod()
        {
            return typeof(MapSubset).GetMethod(
                nameof(MapSubset.Load),
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string) },
                null
            );
        }

        static bool Prefix(string id, ref MapSubset __result)
        {
            try
            {
                var modPath = NANINukoSubsetPathResolver.GetModPath(id);
                if (!string.IsNullOrEmpty(modPath) && File.Exists(modPath))
                {
                    var subset = GameIO.LoadFile<MapSubset>(modPath);
                    if (subset != null)
                    {
                        __result = subset;
                        Debug.Log("[NANINuko] MapSubset loaded from MOD: " + modPath);
                        return false;
                    }

                    Debug.LogWarning("[NANINuko] MOD subset exists but failed to load: " + modPath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINuko] MapSubset.Load patch failed: " + ex);
            }

            return true;
        }
    }

    [HarmonyPatch]
    internal static class Patch_MapSubset_Exist
    {
        static MethodBase TargetMethod()
        {
            return typeof(MapSubset).GetMethod(
                nameof(MapSubset.Exist),
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string) },
                null
            );
        }

        static bool Prefix(string id, ref bool __result)
        {
            try
            {
                __result = NANINukoSubsetPathResolver.Exists(id);
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINuko] MapSubset.Exist patch failed: " + ex);
                return true;
            }
        }
    }

    internal static class NANINukoSubsetPathResolver
    {
        internal static bool Exists(string subsetId)
        {
            return File.Exists(GetModPath(subsetId)) || File.Exists(GetVanillaPath(subsetId));
        }

        internal static string GetModPath(string subsetId)
        {
            var zone = EClass._zone;
            if (zone == null || string.IsNullOrWhiteSpace(subsetId))
                return null;

            return Path.Combine(GetModRoot(), "Maps", zone.idExport + "_" + subsetId + ".s");
        }

        internal static string GetVanillaPath(string subsetId)
        {
            var zone = EClass._zone;
            if (zone == null || string.IsNullOrWhiteSpace(subsetId))
                return null;

            return Path.Combine(CorePath.ZoneSave, zone.idExport + "_" + subsetId + ".s");
        }

        private static string GetModRoot()
        {
            var asm = typeof(NANINukoMainPlugin).Assembly;
            var dir = Path.GetDirectoryName(asm.Location);
            return string.IsNullOrEmpty(dir) ? AppDomain.CurrentDomain.BaseDirectory : dir;
        }
    }
}
