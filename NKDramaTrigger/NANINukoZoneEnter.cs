using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace NANINuko.Framework.Patches
{
    [HarmonyPatch]
    internal static class Patch_Chara_MoveZone_NANINukoZoneEnter
    {
        static MethodBase TargetMethod()
        {
            var charaType = AccessTools.TypeByName("Chara");
            var zoneType = AccessTools.TypeByName("Zone");
            var transitionType = AccessTools.TypeByName("ZoneTransition");

            if (charaType == null || zoneType == null || transitionType == null)
            {
                Debug.LogWarning("[NANINuko] Patch target types not found for Chara.MoveZone.");
                return null;
            }

            var method = AccessTools.Method(
                charaType,
                "MoveZone",
                new[] { zoneType, transitionType }
            );

            if (method == null)
            {
                Debug.LogWarning("[NANINuko] Target method not found: Chara.MoveZone(Zone, ZoneTransition)");
            }

            return method;
        }

        static void Postfix(
            object __instance,
            [HarmonyArgument("z")] object zone,
            [HarmonyArgument("transition")] object transition)
        {
            try
            {
                if (__instance == null || zone == null)
                    return;

                var host = NANINuko.Framework.NANINukoFrameworkPlugin.Instance;
                if (host != null)
                {
                    host.StartCoroutine(DelayZoneEnterNextFrame(__instance, zone));
                    return;
                }

                NANINukoBootstrap.OnZoneEnter(__instance, zone);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINuko] Zone enter patch failed: " + ex);
            }
        }

        private static IEnumerator DelayZoneEnterNextFrame(object chara, object zone)
        {
            yield return null;

            try
            {
                NANINukoBootstrap.OnZoneEnter(chara, zone);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINuko] Delayed zone enter failed: " + ex);
            }
        }
    }
}
