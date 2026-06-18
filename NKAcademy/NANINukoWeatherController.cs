using System;
using UnityEngine;
using GameWeather = global::Weather;
using GameCondition = global::Weather.Condition;

namespace NANINuko.ElinWeather
{
    public static class NANINukoWeatherController
    {
        private static bool _initialized;
        private static bool _lockEnabled;
        private static GameCondition? _lockedCondition;
        private static GameCondition? _previousCondition;

        internal static void InitializeIfNeeded()
        {
            if (_initialized)
                return;

            if (EClass.world == null || EClass.world.weather == null)
            {
                Debug.LogWarning("[NANINukoWeatherController] World weather not ready.");
                return;
            }

            _initialized = true;
            Debug.Log("[NANINukoWeatherController] Initialized successfully.");
        }

        public static void SetAndLockWeather(string weatherName)
        {
            InitializeIfNeeded();
            if (!_initialized)
                return;

            try
            {
                var weather = GetWeather();
                if (weather == null)
                    throw new InvalidOperationException("Weather not ready.");

                if (!_previousCondition.HasValue)
                {
                    _previousCondition = weather.CurrentCondition;
                }

                var condition = MapWeatherByName(weatherName);

                _lockedCondition = condition;
                _lockEnabled = true;

                ApplyWeather(condition);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINukoWeatherController] SetAndLockWeather failed: " + ex);
            }
        }

        public static void UnlockWeather()
        {
            InitializeIfNeeded();
            if (!_initialized)
                return;

            try
            {
                if (_previousCondition.HasValue)
                {
                    ApplyWeather(_previousCondition.Value, true);
                }
            }
            catch
            {
            }

            _lockEnabled = false;
            _lockedCondition = null;

            try
            {
                RefreshSkySafe();
            }
            catch
            {
            }

            _previousCondition = null;
        }

        public static void Tick()
        {
            if (!_lockEnabled || !_lockedCondition.HasValue)
                return;

            InitializeIfNeeded();
            if (!_initialized)
                return;

            try
            {
                ApplyWeather(_lockedCondition.Value, true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINukoWeatherController] Tick failed: " + ex);
            }
        }

        private static GameWeather GetWeather()
        {
            return EClass.world?.weather;
        }

        private static void ApplyWeather(GameCondition targetCondition, bool onlyIfDifferent = false)
        {
            var weather = GetWeather();
            if (weather == null)
                throw new InvalidOperationException("Weather not ready.");

            if (onlyIfDifferent && weather.CurrentCondition == targetCondition)
                return;

            weather.SetCondition(targetCondition, 20, false);
            RefreshSkySafe();
        }

        private static GameCondition MapWeatherByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return GameCondition.Fine;

            switch (name.Trim().ToLowerInvariant())
            {
                case "fine":
                    return GameCondition.Fine;
                case "cloudy":
                    return GameCondition.Cloudy;
                case "rain":
                    return GameCondition.Rain;
                case "rainheavy":
                    return GameCondition.RainHeavy;
                case "ether":
                    return GameCondition.Ether;
                case "blossom":
                    return GameCondition.Blossom;
                case "snow":
                    return GameCondition.Snow;
                case "snowheavy":
                    return GameCondition.SnowHeavy;
                default:
                    return GameCondition.Fine;
            }
        }

        private static void RefreshSkySafe()
        {
            try
            {
                EClass.core?.screen?.RefreshSky();
                return;
            }
            catch
            {
            }

            try
            {
                EClass.screen?.RefreshSky();
            }
            catch
            {
            }
        }
    }
}
