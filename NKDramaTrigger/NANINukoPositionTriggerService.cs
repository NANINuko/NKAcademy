using System;
using UnityEngine;
using NANINuko.Framework.Data;

namespace NANINuko.Framework.Runtime
{
    public static class NANINukoPositionTriggerService
    {
        public static bool TryProcessCurrent(NANINukoRuntimeDatabase database, object pcObj, object zoneObj)
        {
            if (database == null || !database.IsLoaded)
                return false;

            var pc = pcObj as Chara;
            var zone = zoneObj as Zone;
            if (pc == null || zone == null)
                return false;

            var zoneId = zone.idExport;
            if (string.IsNullOrWhiteSpace(zoneId))
                return false;

            var defs = database.GetPositionEvents(zoneId);
            if (defs == null || defs.Count == 0)
                return false;

            var pos = pc.pos;
            if (pos == null)
                return false;

            var x = pos.x;
            var z = pos.z;

            foreach (var def in defs)
            {
                if (def == null)
                    continue;

                if (def.TriggerType != NANINukoTriggerType.Position)
                    continue;

                if (!NANINukoTriggerEvaluator.CanTrigger(def))
                    continue;

                if (!IsInRange(x, z, def.XMin, def.XMax, def.ZMin, def.ZMax))
                    continue;

                if (NANINukoTriggerEvaluator.TryExecute(def))
                    return true;
            }

            return false;
        }

        private static bool IsInRange(int x, int z, int xMin, int xMax, int zMin, int zMax)
        {
            return x >= xMin && x <= xMax && z >= zMin && z <= zMax;
        }
    }
}
