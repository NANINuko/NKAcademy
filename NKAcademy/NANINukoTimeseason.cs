using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using NANINuko.Framework.Runtime;

namespace NANINuko
{
    public static class NANINukoAcademyTimeSeasonFlagModule
    {
        public const string SeasonFlag = "naninu.academy.season";
        public const string TimeFlag = "naninu.academy.time";

        private static bool _initialized;
        private static int _lastSeason = -1;
        private static int _lastTime = -1;

        public static void Init(Harmony harmony)
        {
            if (_initialized)
                return;

            _initialized = true;
            harmony.PatchAll(typeof(Patch_GameDate_AdvanceHour_TimeFlags));
            Debug.Log("[NANINukoAcademyTimeSeasonFlagModule] Initialized");
        }

        public static void SyncNow()
        {
            try
            {
                if (EClass.core == null || EClass.game == null || EClass.player == null || EClass.world == null)
                    return;

                var date = EClass.world.date;
                if (date == null)
                    return;

                Apply(date.month, date.hour);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINukoAcademyTimeSeasonFlagModule] SyncNow failed: " + ex);
            }
        }
        [HarmonyPatch]
        private class Patch_GameDate_AdvanceHour_TimeFlags
        {
            static MethodBase TargetMethod()
            {
                var t = AccessTools.TypeByName("GameDate");
                return t != null ? AccessTools.Method(t, "AdvanceHour") : null;
            }

            static void Postfix()
            {
                SyncNow();
            }
        }

        private static void Apply(int month, int hour)
        {
            int seasonValue;
            if (month >= 3 && month <= 5) seasonValue = 1;
            else if (month >= 6 && month <= 8) seasonValue = 2;
            else if (month >= 9 && month <= 11) seasonValue = 3;
            else seasonValue = 4;

            int timeValue;
            if (hour >= 5 && hour < 11) timeValue = 1;
            else if (hour >= 11 && hour < 17) timeValue = 2;
            else timeValue = 3;

            SetIfChanged(SeasonFlag, seasonValue, ref _lastSeason);
            SetIfChanged(TimeFlag, timeValue, ref _lastTime);
        }

        private static void SetIfChanged(string flagId, int value, ref int cache)
        {
            if (cache == value)
                return;

            NANINukoFlagStore.SetInt(flagId, value);
            cache = value;
        }
    }
}