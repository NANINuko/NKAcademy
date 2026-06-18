using System;
using UnityEngine;
using NANINuko.Framework.Data;

namespace NANINuko.Framework.Runtime
{
    public static class NANINukoZoneTriggerService
    {
        public static bool TryProcessEnter(NANINukoRuntimeDatabase database, object chara, object zone)
        {
            if (database == null || !database.IsLoaded)
                return false;

            var pc = chara as Chara;
            if (pc == null)
                return false;

            if (!pc.IsPC)
                return false;

            var z = zone as Zone;
            if (z == null)
                return false;

            var zoneId = z.idExport;
            if (string.IsNullOrWhiteSpace(zoneId))
                return false;

            var defs = database.GetZoneEvents(zoneId);
            if (defs == null || defs.Count == 0)
                return false;

            return NANINukoTriggerEvaluator.TryExecuteFirstMatch(defs);
        }
    }
}

