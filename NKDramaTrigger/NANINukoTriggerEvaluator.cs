using System;
using System.Collections.Generic;
using NANINuko.Framework.Data;

namespace NANINuko.Framework.Runtime
{
    public static class NANINukoTriggerEvaluator
    {
        public static bool CanTrigger(NANINukoEventDefinition def)
        {
            if (def == null) return false;
            if (!def.Enabled) return false;

            if (!NANINukoFlagStore.MatchesPreFlags(def.PreFlags))
                return false;

            if (!string.IsNullOrWhiteSpace(def.SetFlag) &&
                NANINukoFlagStore.GetBool(def.SetFlag))
            {
                return false;
            }

            return true;
        }

        public static bool TryExecute(NANINukoEventDefinition def)
        {
            if (!CanTrigger(def))
                return false;

            if (!NANINukoDramaInvoker.Play(def))
                return false;

            if (!string.IsNullOrWhiteSpace(def.SetFlag))
            {
                NANINukoFlagStore.SetBool(def.SetFlag, true);
            }

            return true;
        }

        public static bool TryExecuteFirstMatch(IEnumerable<NANINukoEventDefinition> defs)
        {
            if (defs == null)
                return false;

            foreach (var def in defs)
            {
                if (TryExecute(def))
                    return true;
            }

            return false;
        }
    }
}
