using System;
using System.Collections.Generic;
using Cwl.API.Drama;
using UnityEngine;

namespace NANINuko.Cwl
{
    public class NANINukoUninstallCwlAddon : DramaOutcome
    {
        public static bool academy_uninstall(
            DramaManager dm,
            Dictionary<string, string> line,
            params string[] parameters)
        {
            try
            {
                return NANINukoUninstallService.Run();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINuko] academy_uninstall failed: " + ex);
                return false;
            }
        }
    }
}
