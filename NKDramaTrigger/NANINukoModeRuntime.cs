using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using NANINuko.Framework.Runtime;

namespace NANINuko.Framework
{
    public enum NANINukoMode
    {
        Full = 0,
        ContentOnly = 1,
        Off = 2
    }

    public static class NANINukoModeRuntime
    {
        public const string ContentOnlyFlagId = "naninu.academy.mode.content_only";
        public const string LegacyContentOnlyFlagId = "naninu.mod.content_only";

        private const string ModeSection = "General";
        private const string ModeKey = "Mode";
        private const string ModeDescription =
            "Full=全部有効 / ContentOnly=キャラ追加・zone定義だけ / Off=無効。"
            + "起動時に一度決定され、NANINuko.mode.lock がある間は固定。"
            + "変更するには lock file を削除してください。";

        private const string ModeLockFileName = "NANINuko.mode.lock";

        private static readonly object _gate = new object();
        private static NANINukoMode _current = NANINukoMode.Full;
        private static bool _loaded;
        private static ConfigEntry<NANINukoMode> _attachedEntry;

        public static NANINukoMode Current
        {
            get { return EnsureLoaded(); }
        }

        public static bool IsFull => Current == NANINukoMode.Full;
        public static bool IsContentOnly => Current == NANINukoMode.ContentOnly;
        public static bool IsOff => Current == NANINukoMode.Off;

        public static void AttachModeEntry(ConfigEntry<NANINukoMode> entry)
        {
            lock (_gate)
            {
                _attachedEntry = entry;
            }
        }

        public static NANINukoMode EnsureLoaded()
        {
            lock (_gate)
            {
                if (_loaded)
                    return _current;

                _current = LoadEffectiveMode();
                _loaded = true;
                return _current;
            }
        }

        public static bool SyncContentOnlyFlagIfPossible()
        {
            try
            {
                var flags = EClass.player?.dialogFlags;
                if (flags == null)
                    return false;

                var mode = EnsureLoaded();
                bool changed = false;

                if (mode == NANINukoMode.ContentOnly)
                {
                    if (!flags.TryGetValue(ContentOnlyFlagId, out int current) || current != 1)
                    {
                        NANINukoFlagStore.SetBool(ContentOnlyFlagId, true);
                        changed = true;
                    }
                    else
                    {
                        NANINukoFlagStore.SetBool(ContentOnlyFlagId, true);
                    }

                    if (flags.ContainsKey(LegacyContentOnlyFlagId))
                    {
                        NANINukoFlagStore.RemoveFlag(LegacyContentOnlyFlagId);
                        changed = true;
                    }

                    return changed;
                }

                if (flags.ContainsKey(ContentOnlyFlagId))
                {
                    NANINukoFlagStore.RemoveFlag(ContentOnlyFlagId);
                    changed = true;
                }
                else
                {
                    NANINukoFlagStore.RemoveFlag(ContentOnlyFlagId);
                }

                if (flags.ContainsKey(LegacyContentOnlyFlagId))
                {
                    NANINukoFlagStore.RemoveFlag(LegacyContentOnlyFlagId);
                    changed = true;
                }
                else
                {
                    NANINukoFlagStore.RemoveFlag(LegacyContentOnlyFlagId);
                }

                return changed;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static bool DeleteModeLockFile()
        {
            try
            {
                var lockPath = GetModeLockPath();
                if (string.IsNullOrWhiteSpace(lockPath))
                    return false;

                if (File.Exists(lockPath))
                    File.Delete(lockPath);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINukoModeRuntime] DeleteModeLockFile failed: " + ex);
                return false;
            }
        }

        private static NANINukoMode LoadEffectiveMode()
        {
            var lockPath = GetModeLockPath();

            if (TryReadModeLock(lockPath, out var lockedMode))
                return lockedMode;

            var mode = ReadModeFromSource();
            TryWriteModeLock(lockPath, mode);
            return mode;
        }

        private static NANINukoMode ReadModeFromSource()
        {
            try
            {
                var attached = _attachedEntry;
                if (attached != null)
                    return attached.Value;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINukoModeRuntime] Read attached mode failed: " + ex);
            }

            return ReadModeFromConfigFile(GetFrameworkConfigPath());
        }

        private static NANINukoMode ReadModeFromConfigFile(string cfgPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cfgPath))
                    return NANINukoMode.Full;

                var cfg = new ConfigFile(cfgPath, false);
                var entry = cfg.Bind(
                    ModeSection,
                    ModeKey,
                    NANINukoMode.Full,
                    ModeDescription
                );

                return entry.Value;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINukoModeRuntime] ReadModeFromConfigFile failed: " + ex);
                return NANINukoMode.Full;
            }
        }

        private static bool TryReadModeLock(string lockPath, out NANINukoMode mode)
        {
            mode = NANINukoMode.Full;

            try
            {
                if (string.IsNullOrWhiteSpace(lockPath) || !File.Exists(lockPath))
                    return false;

                var text = File.ReadAllText(lockPath).Trim();
                if (TryParseMode(text, out mode))
                    return true;

                Debug.LogWarning("[NANINukoModeRuntime] Invalid mode lock content: " + text);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINukoModeRuntime] TryReadModeLock failed: " + ex);
            }

            return false;
        }

        private static void TryWriteModeLock(string lockPath, NANINukoMode mode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(lockPath))
                    return;

                var dir = Path.GetDirectoryName(lockPath);
                if (!string.IsNullOrWhiteSpace(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(lockPath, mode.ToString());
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINukoModeRuntime] TryWriteModeLock failed: " + ex);
            }
        }

        private static bool TryParseMode(string text, out NANINukoMode mode)
        {
            if (Enum.TryParse(text, true, out mode))
                return true;

            mode = NANINukoMode.Full;
            return false;
        }

        private static string GetFrameworkConfigPath()
        {
            try
            {
                var instance = NANINukoFrameworkPlugin.Instance;
                if (instance != null)
                {
                    var config = instance.Config;
                    if (config != null)
                    {
                        var t = config.GetType();

                        var prop = t.GetProperty(
                            "ConfigFilePath",
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                        );
                        if (prop != null)
                        {
                            var path = prop.GetValue(config, null) as string;
                            if (!string.IsNullOrWhiteSpace(path))
                                return path;
                        }

                        prop = t.GetProperty(
                            "FilePath",
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                        );
                        if (prop != null)
                        {
                            var path = prop.GetValue(config, null) as string;
                            if (!string.IsNullOrWhiteSpace(path))
                                return path;
                        }

                        var field = t.GetField(
                            "ConfigFilePath",
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                        );
                        if (field != null)
                        {
                            var path = field.GetValue(config) as string;
                            if (!string.IsNullOrWhiteSpace(path))
                                return path;
                        }

                        field = t.GetField(
                            "FilePath",
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                        );
                        if (field != null)
                        {
                            var path = field.GetValue(config) as string;
                            if (!string.IsNullOrWhiteSpace(path))
                                return path;
                        }
                    }
                }
            }
            catch
            {
            }

            try
            {
                var baseDir = Paths.ConfigPath;
                if (string.IsNullOrWhiteSpace(baseDir))
                    baseDir = AppDomain.CurrentDomain.BaseDirectory ?? string.Empty;

                return Path.Combine(baseDir, NANINukoFrameworkPlugin.PluginGuid + ".cfg");
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string GetModeLockPath()
        {
            try
            {
                var baseDir = Paths.ConfigPath;
                if (string.IsNullOrWhiteSpace(baseDir))
                    baseDir = AppDomain.CurrentDomain.BaseDirectory ?? string.Empty;

                return Path.Combine(baseDir, ModeLockFileName);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
