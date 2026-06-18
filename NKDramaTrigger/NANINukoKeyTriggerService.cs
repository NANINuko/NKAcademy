using System;
using System.Collections.Generic;
using UnityEngine;
using NANINuko.Framework.Data;

namespace NANINuko.Framework.Runtime
{
    public static class NANINukoKeyTriggerService
    {
        private static readonly Dictionary<string, KeyCode?> KeyCache =
            new Dictionary<string, KeyCode?>(StringComparer.OrdinalIgnoreCase);

        public static bool Tick(NANINukoRuntimeDatabase database)
        {
            if (database == null || !database.IsLoaded)
                return false;

            return TryProcess(database.AllKeyEvents);
        }

        public static bool TryProcess(IEnumerable<NANINukoEventDefinition> defs)
        {
            if (defs == null)
                return false;

            foreach (var def in defs)
            {
                if (def == null)
                    continue;

                if (def.TriggerType != NANINukoTriggerType.Key)
                    continue;

                if (!TryGetKeyCode(def.KeyName, out var keyCode))
                    continue;

                if (!Input.GetKeyDown(keyCode))
                    continue;

                if (NANINukoTriggerEvaluator.TryExecute(def))
                    return true;
            }

            return false;
        }

        private static bool TryGetKeyCode(string keyName, out KeyCode keyCode)
        {
            keyCode = default;

            keyName = Normalize(keyName);
            if (string.IsNullOrEmpty(keyName))
                return false;

            if (KeyCache.TryGetValue(keyName, out var cached))
            {
                if (cached.HasValue)
                {
                    keyCode = cached.Value;
                    return true;
                }

                return false;
            }

            if (Enum.TryParse(keyName, true, out KeyCode parsed))
            {
                KeyCache[keyName] = parsed;
                keyCode = parsed;
                return true;
            }

            KeyCache[keyName] = null;
            Debug.LogWarning($"[NANINuko] Unknown KeyCode: {keyName}");
            return false;
        }

        private static string Normalize(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? string.Empty : text.Trim();
        }
    }
}
