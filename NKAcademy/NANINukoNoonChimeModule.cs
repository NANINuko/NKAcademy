using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using NANINuko.Framework.Runtime;


namespace NANINuko
{
    public static class NANINukoNoonChimeModule
    {
        private static int _lastPlayedDateKey = -1;

        public static void Init(Harmony harmony)
        {
            _ = harmony;
            Debug.Log("[NANINukoNoonChimeModule] Initialized");
        }

        [HarmonyPatch]
        private class Patch_GameDate_AdvanceHour_NANINukoNoonChime
        {
            static MethodBase TargetMethod()
            {
                var t = AccessTools.TypeByName("GameDate");
                return t != null ? AccessTools.Method(t, "AdvanceHour") : null;
            }

            static void Postfix(object __instance)
            {
                try
                {
                    if (EClass.core == null || EClass.game == null || EClass.pc == null)
                        return;

                    if (EClass._zone == null || EClass._zone.idExport != "chiirin_academy_normal")
                        return;

                    var type = __instance.GetType().BaseType;
                    var hourP = AccessTools.Property(type, "hour");
                    var minP = AccessTools.Property(type, "min");
                    var dayP = AccessTools.Property(type, "day");
                    var monthP = AccessTools.Property(type, "month");
                    var yearP = AccessTools.Property(type, "year");

                    if (hourP == null || minP == null || dayP == null || monthP == null || yearP == null)
                        return;

                    int hour = (int)hourP.GetValue(__instance);
                    int min = (int)minP.GetValue(__instance);
                    int day = (int)dayP.GetValue(__instance);
                    int month = (int)monthP.GetValue(__instance);
                    int year = (int)yearP.GetValue(__instance);

                    if (hour != 12 || min != 0)
                        return;

                    int dateKey = year * 10000 + month * 100 + day;
                    if (_lastPlayedDateKey == dateKey)
                        return;

                    _lastPlayedDateKey = dateKey;

                    Debug.Log("[NANINukoNoonChimeModule] Noon in chiirin_academy_normal.");
                    NANINukoDramaInvoker.Play("Transition_Executor", "noonchime");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[NANINukoNoonChimeModule] Error: " + ex);
                }
            }
        }
    }
}
