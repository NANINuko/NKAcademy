using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using NANINuko.Framework.Runtime;

namespace NANINuko.Framework
{
    public static class NANINukoBootstrap
    {
        private enum InitStage
        {
            WaitingCore,
            WaitingGame,
            WaitingPC,
            Ready
        }

        private static bool _initialized;
        private static readonly List<string> _loadedWorkbookPaths = new List<string>();

        private static InitStage _initStage = InitStage.WaitingCore;

        public static bool IsInitialized => _initialized && NANINukoTriggerService.IsLoaded;
        public static IReadOnlyList<string> LoadedWorkbookPaths => _loadedWorkbookPaths.AsReadOnly();

        public static bool Initialize(string xlsxPath)
        {
            return Initialize(new string[] { xlsxPath });
        }

        public static bool Initialize(IEnumerable<string> xlsxPaths)
        {
            try
            {
                var normalizedPaths = NormalizeWorkbookPaths(xlsxPaths);

                Shutdown();

                if (normalizedPaths.Count == 0)
                {
                    Debug.LogWarning("[NANINuko] Bootstrap initialize skipped: no valid workbook paths.");
                    return false;
                }

                if (!NANINukoTriggerService.LoadFromFiles(normalizedPaths))
                {
                    Debug.LogError("[NANINuko] Bootstrap initialize failed: TriggerService load failed.");
                    return false;
                }

                _loadedWorkbookPaths.Clear();
                _loadedWorkbookPaths.AddRange(normalizedPaths);

                _initialized = true;
                _initStage = InitStage.WaitingCore;

                Debug.Log("[NANINuko] Bootstrap initialized: " + string.Join(" ; ", _loadedWorkbookPaths));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError("[NANINuko] Bootstrap Initialize failed: " + ex);
                Shutdown();
                return false;
            }
        }

        public static bool InitializeFromFolder(string folderPath, string xlsxFileName)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                throw new ArgumentException("folderPath is null or empty.", nameof(folderPath));

            if (string.IsNullOrWhiteSpace(xlsxFileName))
                throw new ArgumentException("xlsxFileName is null or empty.", nameof(xlsxFileName));

            return Initialize(Path.Combine(folderPath, xlsxFileName));
        }

        public static bool Tick()
        {
            if (!IsInitialized)
                return false;

            try
            {
                if (!EnsureGameReady())
                    return false;

                NANINukoFlagStore.ApplyDefaultsToGameIfPossible();
                NANINukoFlagStore.RefreshKnownFlagsFromGameIfPossible();
            }
            catch (Exception ex)
            {
                Debug.LogError("[NANINuko] Bootstrap Tick precheck failed: " + ex);
                return false;
            }

            bool triggered = false;

            try
            {
                triggered |= NANINukoTriggerService.TickKey();

                if (TryGetCurrentPcAndZone(out var pc, out var zone))
                {
                    triggered |= NANINukoTriggerService.TryProcessPosition(pc, zone);
                }

                triggered |= NANINukoTriggerService.TickChara();
            }
            catch (Exception ex)
            {
                Debug.LogError("[NANINuko] Bootstrap Tick failed: " + ex);
            }

            return triggered;
        }

        public static bool OnZoneEnter(object chara, object zone)
        {
            if (!IsInitialized)
                return false;

            if (!EnsureGameReady())
                return false;

            return NANINukoTriggerService.TryProcessZoneEnter(chara, zone);
        }

        public static void Shutdown()
        {
            try
            {
                NANINukoTriggerService.Clear();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINuko] TriggerService clear failed: " + ex);
            }

            _initialized = false;
            _loadedWorkbookPaths.Clear();

            _initStage = InitStage.WaitingCore;
        }

        private static bool EnsureGameReady()
        {
            try
            {
                if (EClass.core == null)
                {
                    _initStage = InitStage.WaitingCore;
                    return false;
                }

                if (EClass.game == null)
                {
                    _initStage = InitStage.WaitingGame;
                    return false;
                }

                if (EClass.pc == null)
                {
                    _initStage = InitStage.WaitingPC;
                    return false;
                }

                if (_initStage != InitStage.Ready)
                {
                    _initStage = InitStage.Ready;
                    Debug.Log("[NANINuko] Game Ready");
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryGetCurrentPcAndZone(out object pc, out object zone)
        {
            pc = EClass.pc;
            zone = EClass._zone;
            return pc != null && zone != null;
        }

        private static List<string> NormalizeWorkbookPaths(IEnumerable<string> workbookPaths)
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (workbookPaths == null)
                return result;

            foreach (var raw in workbookPaths)
            {
                var path = raw?.Trim();
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                try
                {
                    var full = Path.GetFullPath(path);

                    if (!File.Exists(full))
                    {
                        Debug.LogWarning("[NANINuko] Workbook file not found and skipped: " + full);
                        continue;
                    }

                    if (seen.Add(full))
                    {
                        result.Add(full);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[NANINuko] Invalid workbook path skipped: " + path + " (" + ex.Message + ")");
                }
            }

            return result;
        }
    }
}
