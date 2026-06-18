using System;
using System.Reflection;
using Cwl.API.Custom;
using HarmonyLib;
using UnityEngine;

namespace NANINuko
{
    [HarmonyPatch]
    public static class NANINukoAchievementPatch
    {
        static MethodBase TargetMethod()
        {
            return typeof(FoodEffect).GetMethod(
                nameof(FoodEffect.Proc),
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(Chara), typeof(Thing), typeof(bool) },
                null
            );
        }

        static void Postfix(Chara c, Thing food)
        {
            try
            {
                if (c == null || food == null)
                    return;

                if (NANINukoAchievementState.Unlocked)
                    return;

                if (!c.IsPC)
                    return;

                if (food.id != "8915")
                    return;

                CustomAchievement.Unlock("naninu_gourmet");

                NANINukoAchievementState.Unlocked = true;
                Debug.Log("[NANINuko] Food 8915 eaten -> naninu_gourmet unlocked");
            }
            catch (Exception ex)
            {
                Debug.LogError("[NANINuko] Food8915 achievement error: " + ex);
            }
        }
    }

    public static class NANINukoAchievementState
    {
        public static bool Unlocked;
    }
}
