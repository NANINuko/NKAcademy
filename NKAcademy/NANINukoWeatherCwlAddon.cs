using System;
using System.Collections.Generic;
using Cwl.API.Drama;
using UnityEngine;

namespace NANINuko.ElinWeather
{
    public class NANINukoWeatherCwlAddon : DramaOutcome
    {
        public static bool weather_set(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
        {
            try
            {
                var weatherName = GetSingleArg(line, parameters);
                if (string.IsNullOrWhiteSpace(weatherName))
                    weatherName = "Fine";

                NANINukoWeatherController.SetAndLockWeather(weatherName);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINuko Weather] weather_set failed: " + ex);
                return false;
            }
        }

        public static bool weather_unlock(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
        {
            try
            {
                NANINukoWeatherController.UnlockWeather();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINuko Weather] weather_unlock failed: " + ex);
                return false;
            }
        }

        private static string GetSingleArg(Dictionary<string, string> line, string[] parameters)
        {
            if (parameters != null && parameters.Length > 0)
                return Normalize(parameters[0]);

            if (line != null && line.TryGetValue("param", out var raw))
                return Normalize(raw);

            return string.Empty;
        }

        private static string Normalize(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? string.Empty : text.Trim();
        }
    }
}