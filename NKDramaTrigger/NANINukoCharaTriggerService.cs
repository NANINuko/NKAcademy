using System;
using UnityEngine;
using NANINuko.Framework.Data;

namespace NANINuko.Framework.Runtime
{
    public static class NANINukoCharaTriggerService
    {
        public static bool Tick(NANINukoRuntimeDatabase database)
        {
            if (database == null || !database.IsLoaded)
                return false;

            var zone = EClass._zone;
            if (zone == null)
                return false;

            var map = EClass._map;
            if (map == null)
                return false;

            var zoneId = zone.idExport;
            if (string.IsNullOrWhiteSpace(zoneId))
                return false;

            var events = database.AllCharaEvents;
            if (events == null || events.Count == 0)
                return false;

            foreach (var def in events)
            {
                if (def == null)
                    continue;

                if (def.TriggerType != NANINukoTriggerType.Chara)
                    continue;

                if (!string.Equals(def.ZoneId?.Trim(), zoneId, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!NANINukoTriggerEvaluator.CanTrigger(def))
                    continue;

                if (!TryFindChara(map, def.CharaId, out var chara))
                    continue;

                if (!IsTargetStateMatched(chara, def, out var matched))
                    continue;

                if (!matched)
                    continue;

                if (NANINukoTriggerEvaluator.TryExecute(def))
                    return true;
            }

            return false;
        }

        private static bool IsTargetStateMatched(Chara chara, NANINukoEventDefinition def, out bool matched)
        {
            matched = false;

            if (def == null)
                return false;

            switch (def.CharaDetectType)
            {
                case NANINukoCharaDetectType.Dead:
                    if (chara == null)
                    {
                        matched = true;
                        return true;
                    }

                    matched = chara.isDead;
                    return true;

                case NANINukoCharaDetectType.Missing:
                    matched = chara == null;
                    return true;

                default:
                    return false;
            }
        }

        private static bool TryFindChara(Map map, string charaId, out Chara chara)
        {
            chara = null;

            if (map == null || string.IsNullOrWhiteSpace(charaId))
                return false;

            chara = map.FindChara(charaId.Trim());
            return true;
        }
    }
}
