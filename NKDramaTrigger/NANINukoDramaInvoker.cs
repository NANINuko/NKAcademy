using System;
using UnityEngine;
using NANINuko.Framework.Data;

namespace NANINuko.Framework.Runtime
{
    public static class NANINukoDramaInvoker
    {
        public static bool Play(string sheet, string step)
        {
            try
            {
                sheet = Normalize(sheet);
                step = Normalize(step);

                if (string.IsNullOrEmpty(sheet))
                {
                    Debug.LogWarning("[NANINuko] Play failed: sheet is empty.");
                    return false;
                }

                var pc = EClass.pc;
                if (pc == null)
                {
                    Debug.LogWarning("[NANINuko] Play failed: pc is null.");
                    return false;
                }

                var prevForceJump = LayerDrama.forceJump;

                try
                {
                    LayerDrama.forceJump = string.IsNullOrEmpty(step) ? null : step;
                    pc.ShowDialog(sheet);
                }
                finally
                {
                    LayerDrama.forceJump = prevForceJump;
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINuko] Play failed: " + ex);
                return false;
            }
        }

        public static bool Play(NANINukoEventDefinition def)
        {
            if (def == null)
                return false;

            return Play(def.DramaSheet, def.DramaStep);
        }

        private static string Normalize(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? string.Empty : text.Trim();
        }
    }
}
