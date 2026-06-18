using System.Text;
using HarmonyLib;
using UnityEngine;

namespace NANINuko
{
    internal static class NANINukoTextReplacer
    {
        public static string Apply(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var teamName = NANINukoConfig.TeamName?.Value;
            if (string.IsNullOrEmpty(teamName))
                teamName = "チーム名未設定";

            var honorific = NANINukoConfig.Honorific?.Value;
            if (honorific == null)
                honorific = "さん";

            return text
                .Replace("#teamname", teamName)
                .Replace("#honorific", honorific);
        }
    }

    [HarmonyPatch(typeof(GameLang), nameof(GameLang.Convert), new[] { typeof(string) })]
    internal static class Patch_GameLang_Convert_String
    {
        static void Postfix(ref string __result)
        {
            try
            {
                __result = NANINukoTextReplacer.Apply(__result);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[NANINuko] GameLang.Convert(string) patch failed: " + ex);
            }
        }
    }

    [HarmonyPatch(typeof(GameLang), nameof(GameLang.ConvertDrama), new[] { typeof(string), typeof(Chara) })]
    internal static class Patch_GameLang_ConvertDrama
    {
        static void Postfix(ref string __result)
        {
            try
            {
                __result = NANINukoTextReplacer.Apply(__result);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[NANINuko] GameLang.ConvertDrama patch failed: " + ex);
            }
        }
    }

    [HarmonyPatch(typeof(GameLang), nameof(GameLang.Convert), new[] { typeof(StringBuilder) })]
    internal static class Patch_GameLang_Convert_StringBuilder
    {
        static void Postfix(ref string __result)
        {
            try
            {
                __result = NANINukoTextReplacer.Apply(__result);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[NANINuko] GameLang.Convert(StringBuilder) patch failed: " + ex);
            }
        }
    }

    [HarmonyPatch(typeof(DialogDrama), nameof(DialogDrama.SetText))]
    internal static class Patch_DialogDrama_SetText
    {
        static void Prefix(ref string detail)
        {
            try
            {
                detail = NANINukoTextReplacer.Apply(detail);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[NANINuko] DialogDrama.SetText patch failed: " + ex);
            }
        }
    }

    [HarmonyPatch(typeof(UIText), nameof(UIText.SetText), new[] { typeof(string) })]
    internal static class Patch_UIText_SetText
    {
        static void Prefix(ref string s)
        {
            try
            {
                s = NANINukoTextReplacer.Apply(s);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[NANINuko] UIText.SetText patch failed: " + ex);
            }
        }
    }
}