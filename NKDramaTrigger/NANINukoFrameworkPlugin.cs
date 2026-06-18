using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace NANINuko.Framework
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class NANINukoFrameworkPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.naninuko.framework";
        public const string PluginName = "NANINuko Framework";
        public const string PluginVersion = "0.1.0";

        public static NANINukoFrameworkPlugin Instance { get; private set; }

        private static readonly object _registryLock = new object();
        private static readonly List<string> _registeredWorkbookPaths = new List<string>();
        private static readonly HashSet<string> _registeredWorkbookPathSet =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static bool _pendingWorkbookReload;

        private Harmony _harmony;
        private bool _initialized;
        private bool _shutdownRequested;
        private bool _shutdownCompleted;

        private ConfigEntry<NANINukoMode> _mode;
        private ConfigEntry<string> _workbookPaths;
        private ConfigEntry<string> _legacyWorkbookPath;

        private void Awake()
        {
            try
            {
                Instance = this;

                Logger.LogInfo("[NANINuko] Framework loading...");

                BindModeConfig();
                NANINukoModeRuntime.AttachModeEntry(_mode);

                var mode = NANINukoModeRuntime.EnsureLoaded();
                Logger.LogInfo("[NANINuko] Framework mode: " + mode);

                if (mode == NANINukoMode.Off)
                {
                    Logger.LogInfo("[NANINuko] Mode=Off, framework disabled.");
                    return;
                }

                if (mode == NANINukoMode.ContentOnly)
                {
                    Logger.LogInfo("[NANINuko] ContentOnly mode: no Harmony patches.");
                    return;
                }

                BindWorkbookConfigs();

                _harmony = new Harmony(PluginGuid);
                _harmony.PatchAll();

                SyncWorkbookPathsConfig(forceSave: true);

                var workbookPaths = ResolveWorkbookPaths();
                if (workbookPaths.Count == 0)
                {
                    Logger.LogWarning("[NANINuko] WorkbookPaths is empty. Framework is loaded but not initialized.");
                    _initialized = false;
                    return;
                }

                Logger.LogInfo("[NANINuko] Workbook files:");
                foreach (var path in workbookPaths)
                {
                    Logger.LogInfo("  - " + path);
                }

                _initialized = NANINukoBootstrap.Initialize(workbookPaths);
                if (_initialized)
                {
                    Logger.LogInfo("[NANINuko] Framework initialized.");
                }
                else
                {
                    Logger.LogError("[NANINuko] Bootstrap initialization failed.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("[NANINuko] Awake failed: " + ex);
                _initialized = false;
            }
        }

        private void Update()
        {
            if (_shutdownRequested)
                return;

            NANINukoModeRuntime.SyncContentOnlyFlagIfPossible();

            if (!NANINukoModeRuntime.IsFull)
                return;

            if (_pendingWorkbookReload)
            {
                _pendingWorkbookReload = false;
                ReloadFromCurrentWorkbookPaths();
            }

            if (!_initialized)
                return;

            NANINukoBootstrap.Tick();
        }

        public static bool RegisterWorkbookPath(string workbookPath, bool reloadIfLoaded = true)
        {
            return RegisterWorkbookPathsInternal(
                Assembly.GetCallingAssembly(),
                new[] { workbookPath },
                reloadIfLoaded
            );
        }

        public static bool RegisterWorkbookPaths(IEnumerable<string> workbookPaths, bool reloadIfLoaded = true)
        {
            return RegisterWorkbookPathsInternal(
                Assembly.GetCallingAssembly(),
                workbookPaths,
                reloadIfLoaded
            );
        }

        public static bool UnregisterWorkbookPath(string workbookPath, bool reloadIfLoaded = true)
        {
            return UnregisterWorkbookPathsInternal(
                Assembly.GetCallingAssembly(),
                new[] { workbookPath },
                reloadIfLoaded
            );
        }

        public static bool UnregisterWorkbookPaths(IEnumerable<string> workbookPaths, bool reloadIfLoaded = true)
        {
            return UnregisterWorkbookPathsInternal(
                Assembly.GetCallingAssembly(),
                workbookPaths,
                reloadIfLoaded
            );
        }

        public static IReadOnlyList<string> GetRegisteredWorkbookPaths()
        {
            lock (_registryLock)
            {
                return new List<string>(_registeredWorkbookPaths).AsReadOnly();
            }
        }

        public static IReadOnlyList<string> GetEffectiveWorkbookPaths()
        {
            return BuildEffectiveWorkbookPaths(Instance).AsReadOnly();
        }

        public static void ReloadFromRegisteredWorkbookPaths()
        {
            if (Instance == null)
                return;

            Instance.ReloadFromCurrentWorkbookPaths();
        }

        public static void ShutdownForUninstall()
        {
            if (Instance == null)
                return;

            Instance.ShutdownInternal(clearRegistry: false);
        }

        private void OnDestroy()
        {
            try
            {
                ShutdownInternal(clearRegistry: true);
            }
            catch (Exception ex)
            {
                Logger.LogWarning("[NANINuko] Bootstrap shutdown failed: " + ex);
            }

            lock (_registryLock)
            {
                _registeredWorkbookPaths.Clear();
                _registeredWorkbookPathSet.Clear();
            }

            if (Instance == this)
                Instance = null;

            Logger.LogInfo("[NANINuko] Framework unloaded.");
        }

        private void ShutdownInternal(bool clearRegistry)
        {
            if (_shutdownCompleted)
                return;

            _shutdownRequested = true;
            _pendingWorkbookReload = false;
            _initialized = false;
            _shutdownCompleted = true;

            try
            {
                NANINukoBootstrap.Shutdown();
            }
            catch (Exception ex)
            {
                Logger.LogWarning("[NANINuko] Bootstrap shutdown failed: " + ex);
            }

            try
            {
                _harmony?.UnpatchSelf();
            }
            catch (Exception ex)
            {
                Logger.LogWarning("[NANINuko] Unpatch failed: " + ex);
            }

            enabled = false;

            if (clearRegistry)
            {
                lock (_registryLock)
                {
                    _registeredWorkbookPaths.Clear();
                    _registeredWorkbookPathSet.Clear();
                }
            }
        }

        private void BindModeConfig()
        {
            if (_mode != null)
                return;

            _mode = Config.Bind(
                "General",
                "Mode",
                NANINukoMode.Full,
                "Full=全部有効 / ContentOnly=キャラ追加・zone定義だけ / Off=無効。起動時に一度決定され、NANINuko.mode.lock がある間は固定。変更するには lock file を削除してください。"
            );
        }

        private void BindWorkbookConfigs()
        {
            if (_workbookPaths != null || _legacyWorkbookPath != null)
                return;

            _workbookPaths = Config.Bind(
                "General",
                "WorkbookPaths",
                string.Empty,
                "NANINuko の .xlsx ファイルを ; または改行区切りで複数指定。相対パスは各DLLの場所基準で登録可能。"
            );

            _legacyWorkbookPath = Config.Bind(
                "General",
                "WorkbookPath",
                string.Empty,
                "旧形式の単一WorkbookPath。互換用。"
            );
        }

        private static bool RegisterWorkbookPathsInternal(
            Assembly callerAssembly,
            IEnumerable<string> workbookPaths,
            bool reloadIfLoaded)
        {
            try
            {
                if (workbookPaths == null)
                    throw new ArgumentNullException(nameof(workbookPaths));

                var baseDir = GetAssemblyBaseDirectory(callerAssembly);
                var normalized = NormalizeWorkbookPaths(workbookPaths, baseDir);

                if (normalized.Count == 0)
                    return false;

                bool changed = false;

                lock (_registryLock)
                {
                    foreach (var path in normalized)
                    {
                        if (_registeredWorkbookPathSet.Add(path))
                        {
                            _registeredWorkbookPaths.Add(path);
                            changed = true;
                        }
                    }
                }

                if (!changed)
                    return false;

                Debug.Log(
                    "[NANINuko] WorkbookPaths registered by "
                    + GetAssemblyLabel(callerAssembly)
                    + ": "
                    + string.Join(" ; ", normalized)
                );

                if (Instance != null)
                {
                    Instance.SyncWorkbookPathsConfig(forceSave: true);

                    if (reloadIfLoaded)
                        _pendingWorkbookReload = true;
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINuko] RegisterWorkbookPaths failed: " + ex);
                return false;
            }
        }

        private static bool UnregisterWorkbookPathsInternal(
            Assembly callerAssembly,
            IEnumerable<string> workbookPaths,
            bool reloadIfLoaded)
        {
            try
            {
                if (workbookPaths == null)
                    throw new ArgumentNullException(nameof(workbookPaths));

                var baseDir = GetAssemblyBaseDirectory(callerAssembly);
                var normalized = NormalizeWorkbookPathsForRemoval(workbookPaths, baseDir);

                if (normalized.Count == 0)
                    return false;

                bool changed = false;

                lock (_registryLock)
                {
                    foreach (var path in normalized)
                    {
                        if (_registeredWorkbookPathSet.Remove(path))
                        {
                            _registeredWorkbookPaths.RemoveAll(
                                p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase)
                            );
                            changed = true;
                        }
                    }
                }

                if (Instance != null)
                {
                    changed |= Instance.RemovePathsFromConfig(normalized);

                    if (changed)
                    {
                        Instance.SyncWorkbookPathsConfig(forceSave: true);

                        if (reloadIfLoaded)
                            _pendingWorkbookReload = true;
                    }
                }

                if (!changed)
                    return false;

                Debug.Log(
                    "[NANINuko] WorkbookPaths unregistered by "
                    + GetAssemblyLabel(callerAssembly)
                    + ": "
                    + string.Join(" ; ", normalized)
                );

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINuko] UnregisterWorkbookPaths failed: " + ex);
                return false;
            }
        }

        private void ReloadFromCurrentWorkbookPaths()
        {
            try
            {
                var workbookPaths = ResolveWorkbookPaths();

                if (workbookPaths.Count == 0)
                {
                    NANINukoBootstrap.Shutdown();
                    _initialized = false;
                    Logger.LogWarning("[NANINuko] Reload skipped: no workbook paths available.");
                    return;
                }

                _initialized = NANINukoBootstrap.Initialize(workbookPaths);
                if (_initialized)
                    Logger.LogInfo("[NANINuko] Framework reloaded.");
                else
                    Logger.LogError("[NANINuko] Framework reload failed.");
            }
            catch (Exception ex)
            {
                _initialized = false;
                Logger.LogError("[NANINuko] ReloadFromCurrentWorkbookPaths failed: " + ex);
            }
        }

        private void SyncWorkbookPathsConfig(bool forceSave)
        {
            if (_workbookPaths == null)
                return;

            var workbookPaths = BuildEffectiveWorkbookPaths(this);
            var serialized = SerializeWorkbookPaths(workbookPaths);
            var current = _workbookPaths.Value ?? string.Empty;

            if (!string.Equals(current, serialized, StringComparison.Ordinal))
                _workbookPaths.Value = serialized;

            if (forceSave || !string.Equals(current, serialized, StringComparison.Ordinal))
                Config.Save();
        }

        private bool RemovePathsFromConfig(IEnumerable<string> normalizedTargets)
        {
            var targetSet = new HashSet<string>(normalizedTargets ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            if (targetSet.Count == 0)
                return false;

            var baseDir = GetAssemblyBaseDirectory(Assembly.GetExecutingAssembly());
            bool changed = false;

            if (_legacyWorkbookPath != null)
            {
                var current = _legacyWorkbookPath.Value ?? string.Empty;
                var cleaned = RemovePathsFromSerializedValue(current, targetSet, baseDir);

                if (!string.Equals(current, cleaned, StringComparison.Ordinal))
                {
                    _legacyWorkbookPath.Value = cleaned;
                    changed = true;
                }
            }

            if (_workbookPaths != null)
            {
                var current = _workbookPaths.Value ?? string.Empty;
                var cleaned = RemovePathsFromSerializedValue(current, targetSet, baseDir);

                if (!string.Equals(current, cleaned, StringComparison.Ordinal))
                {
                    _workbookPaths.Value = cleaned;
                    changed = true;
                }
            }

            return changed;
        }

        private List<string> ResolveWorkbookPaths()
        {
            return BuildEffectiveWorkbookPaths(this);
        }

        private static List<string> BuildEffectiveWorkbookPaths(NANINukoFrameworkPlugin plugin)
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (plugin != null)
            {
                var baseDir = GetAssemblyBaseDirectory(Assembly.GetExecutingAssembly());

                AppendWorkbookPaths(result, seen, baseDir, plugin._legacyWorkbookPath?.Value);
                AppendWorkbookPaths(result, seen, baseDir, plugin._workbookPaths?.Value);
            }

            lock (_registryLock)
            {
                foreach (var path in _registeredWorkbookPaths)
                {
                    AddPath(result, seen, path);
                }
            }

            return result;
        }

        private static void AppendWorkbookPaths(
            List<string> result,
            HashSet<string> seen,
            string baseDir,
            string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return;

            var parts = raw.Split(
                new[] { ';', '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries
            );

            foreach (var part in parts)
            {
                var trimmed = part.Trim().Trim('"');
                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                try
                {
                    var full = Path.IsPathRooted(trimmed)
                        ? Path.GetFullPath(trimmed)
                        : Path.GetFullPath(Path.Combine(baseDir, trimmed));

                    AddPath(result, seen, full);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[NANINuko] Invalid workbook path skipped: " + trimmed + " (" + ex.Message + ")");
                }
            }
        }

        private static void AddPath(
            List<string> result,
            HashSet<string> seen,
            string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            var canonical = path.Trim().Trim('"');
            if (seen.Add(canonical))
                result.Add(canonical);
        }

        private static string SerializeWorkbookPaths(IEnumerable<string> workbookPaths)
        {
            if (workbookPaths == null)
                return string.Empty;

            var list = new List<string>();
            foreach (var path in workbookPaths)
            {
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                list.Add(path.Trim());
            }

            return string.Join(";", list);
        }

        private static List<string> NormalizeWorkbookPaths(
            IEnumerable<string> workbookPaths,
            string baseDir)
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (workbookPaths == null)
                return result;

            foreach (var raw in workbookPaths)
            {
                var path = raw?.Trim().Trim('"');
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                try
                {
                    var full = Path.IsPathRooted(path)
                        ? Path.GetFullPath(path)
                        : Path.GetFullPath(Path.Combine(baseDir, path));

                    if (!File.Exists(full))
                    {
                        Debug.LogWarning("[NANINuko] Workbook file not found and skipped: " + full);
                        continue;
                    }

                    if (seen.Add(full))
                        result.Add(full);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[NANINuko] Invalid workbook path skipped: " + path + " (" + ex.Message + ")");
                }
            }

            return result;
        }

        private static List<string> NormalizeWorkbookPathsForRemoval(
            IEnumerable<string> workbookPaths,
            string baseDir)
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (workbookPaths == null)
                return result;

            foreach (var raw in workbookPaths)
            {
                var path = raw?.Trim().Trim('"');
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                try
                {
                    var full = Path.IsPathRooted(path)
                        ? Path.GetFullPath(path)
                        : Path.GetFullPath(Path.Combine(baseDir, path));

                    if (seen.Add(full))
                        result.Add(full);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[NANINuko] Invalid workbook path skipped: " + path + " (" + ex.Message + ")");
                }
            }

            return result;
        }

        private static string RemovePathsFromSerializedValue(
            string raw,
            HashSet<string> targets,
            string baseDir)
        {
            if (string.IsNullOrWhiteSpace(raw) || targets == null || targets.Count == 0)
                return string.Empty;

            var kept = new List<string>();
            var parts = raw.Split(
                new[] { ';', '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries
            );

            foreach (var part in parts)
            {
                var trimmed = part.Trim().Trim('"');
                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                try
                {
                    var full = Path.IsPathRooted(trimmed)
                        ? Path.GetFullPath(trimmed)
                        : Path.GetFullPath(Path.Combine(baseDir, trimmed));

                    if (targets.Contains(full))
                        continue;

                    kept.Add(trimmed);
                }
                catch
                {
                    kept.Add(trimmed);
                }
            }

            return string.Join(";", kept);
        }

        private static string GetAssemblyBaseDirectory(Assembly assembly)
        {
            try
            {
                if (assembly != null)
                {
                    var location = assembly.Location;
                    if (!string.IsNullOrWhiteSpace(location))
                    {
                        var dir = Path.GetDirectoryName(location);
                        if (!string.IsNullOrWhiteSpace(dir))
                            return dir;
                    }
                }
            }
            catch
            {
            }

            return AppDomain.CurrentDomain.BaseDirectory ?? string.Empty;
        }

        private static string GetAssemblyLabel(Assembly assembly)
        {
            try
            {
                return assembly?.GetName().Name ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}
