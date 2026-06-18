using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace NANINuko
{
    public static class NANINukoNoBanishByIdSystem
    {
        private static readonly HashSet<string> NoBanishIds = new HashSet<string>
        {
            "Risae",
        };

        private static bool _initialized;

        public static void Init(Harmony harmony)
        {
            if (_initialized)
                return;

            _initialized = true;

            var banishMethod = typeof(Chara).GetMethod(
                nameof(Chara.Banish),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(Chara) },
                null
            );

            if (banishMethod == null)
            {
                Debug.LogWarning("[NANINukoNoBanishById] Chara.Banish not found. Disabled.");
                return;
            }

            harmony.Patch(
                banishMethod,
                prefix: new HarmonyMethod(typeof(NANINukoNoBanishByIdSystem), nameof(BanishPrefix))
            );

            Debug.Log("[NANINukoNoBanishById] Initialized");
        }

        private static bool BanishPrefix(Chara __instance)
        {
            if (IsNoBanish(__instance))
            {
                Debug.Log("[NANINukoNoBanishById] Blocked banish of protected character");
                return false;
            }

            return true;
        }

        private static bool IsNoBanish(Chara chara)
        {
            if (chara == null)
                return false;

            var id = chara.id;
            if (string.IsNullOrEmpty(id))
                return false;

            return NoBanishIds.Contains(id);
        }
    }
}
