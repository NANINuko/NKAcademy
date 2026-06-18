using System;
using System.Collections.Generic;
using Cwl.API.Drama;
using NANINuko.Reflection;
using UnityEngine;

namespace NANINuko.Cwl
{
    public class NANINukoCharaCwlAddon : DramaOutcome
    {
        public static bool resurrect_chara(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
        {
            try
            {
                var charaId = GetSingleArg(line, parameters);
                if (string.IsNullOrWhiteSpace(charaId))
                {
                    Debug.LogWarning("[NANINuko Cwl] resurrect_chara: param is missing.");
                    return false;
                }

                NANINukoCharaReflectionHelper.ReviveById(charaId);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINuko Cwl] resurrect_chara failed: " + ex);
                return false;
            }
        }

        public static bool make_ally_on_map(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
        {
            try
            {
                var charaId = GetSingleArg(line, parameters);
                if (string.IsNullOrWhiteSpace(charaId))
                {
                    Debug.LogWarning("[NANINuko Cwl] make_ally_on_map: param is missing.");
                    return false;
                }

                NANINukoCharaReflectionHelper.MakeAllyOnMap(charaId);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINuko Cwl] make_ally_on_map failed: " + ex);
                return false;
            }
        }

        public static bool remove_ally_on_map(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
        {
            try
            {
                var charaId = GetSingleArg(line, parameters);
                if (string.IsNullOrWhiteSpace(charaId))
                {
                    Debug.LogWarning("[NANINuko Cwl] remove_ally_on_map: param is missing.");
                    return false;
                }

                NANINukoCharaReflectionHelper.RemoveAllyOnMap(charaId);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINuko Cwl] remove_ally_on_map failed: " + ex);
                return false;
            }
        }

        public static bool move_chara_to(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
        {
            try
            {
                if (!TryGetMoveArgs(line, parameters, out var charaId, out var x, out var z))
                {
                    Debug.LogWarning("[NANINuko Cwl] move_chara_to: param requires id,x,z.");
                    return false;
                }

                NANINukoCharaReflectionHelper.MoveCharaOnMap(charaId, x, z);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINuko Cwl] move_chara_to failed: " + ex);
                return false;
            }
        }

        public static bool NKMove(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
        {
            try
            {
                if (!TryGetNKMoveArgs(line, parameters, out var zoneName, out var x, out var z))
                {
                    Debug.LogWarning("[NANINuko Cwl] NKMove: param requires zoneId,x,z or zoneId,lv,x,z.");
                    return false;
                }

                dm.RequiresActor(out var actor);

                var targetZone = EClass.game?.spatials?.Find(zoneName) as Zone;
                if (targetZone == null)
                {
                    Debug.LogWarning($"[NANINuko Cwl] NKMove: target zone not found: {zoneName}");
                    return false;
                }

                actor.MoveZone(targetZone, new ZoneTransition
                {
                    state = ZoneTransition.EnterState.Exact,
                    x = x,
                    z = z
                });

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINuko Cwl] NKMove failed: " + ex);
                return false;
            }
        }

        private static bool TryGetMoveArgs(
            Dictionary<string, string> line,
            string[] parameters,
            out string charaId,
            out int x,
            out int z)
        {
            charaId = string.Empty;
            x = 0;
            z = 0;

            var args = GetArgs(line, parameters);
            if (args.Length < 3)
                return false;

            charaId = Normalize(args[0]);
            if (string.IsNullOrWhiteSpace(charaId))
                return false;

            return int.TryParse(Normalize(args[1]), out x) &&
                   int.TryParse(Normalize(args[2]), out z);
        }

        private static bool TryGetNKMoveArgs(
            Dictionary<string, string> line,
            string[] parameters,
            out string zoneName,
            out int x,
            out int z)
        {
            zoneName = string.Empty;
            x = 0;
            z = 0;

            var args = GetArgs(line, parameters);
            if (args.Length < 3)
                return false;

            zoneName = Normalize(args[0]);
            if (string.IsNullOrWhiteSpace(zoneName))
                return false;

            // zoneId,x,z
            if (args.Length == 3)
            {
                return int.TryParse(Normalize(args[1]), out x) &&
                       int.TryParse(Normalize(args[2]), out z);
            }

            // zoneId,lv,x,z
            if (args.Length >= 4)
            {
                return int.TryParse(Normalize(args[2]), out x) &&
                       int.TryParse(Normalize(args[3]), out z);
            }

            return false;
        }

        private static string GetSingleArg(Dictionary<string, string> line, string[] parameters)
        {
            var args = GetArgs(line, parameters);
            if (args.Length <= 0)
                return string.Empty;

            return Normalize(args[0]);
        }

        private static string[] GetArgs(Dictionary<string, string> line, string[] parameters)
        {
            if (parameters != null && parameters.Length > 0)
            {
                if (parameters.Length == 1)
                    return SplitCombined(parameters[0]);

                return parameters;
            }

            if (line != null && line.TryGetValue("param", out var raw) && !string.IsNullOrWhiteSpace(raw))
                return SplitCombined(raw);

            return Array.Empty<string>();
        }

        private static string[] SplitCombined(string raw)
        {
            raw = Normalize(raw);
            if (string.IsNullOrEmpty(raw))
                return Array.Empty<string>();

            if (raw.IndexOf(',') >= 0 || raw.IndexOf(' ') >= 0)
                return raw.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            return new[] { raw };
        }

        private static string Normalize(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? string.Empty : text.Trim();
        }
    }
}
