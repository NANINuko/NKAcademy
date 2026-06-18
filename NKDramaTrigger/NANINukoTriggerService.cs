using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using NANINuko.Framework.Excel;

namespace NANINuko.Framework.Runtime
{
    public static class NANINukoTriggerService
    {
        private static NANINukoRuntimeDatabase _database;
        private static int _lastTriggeredFrame = -1;

        public static bool IsLoaded => _database != null && _database.IsLoaded;
        public static NANINukoRuntimeDatabase Database => _database;

        public static bool LoadFromFile(string xlsxPath)
        {
            return LoadFromFiles(new[] { xlsxPath });
        }

        public static bool LoadFromFiles(IEnumerable<string> xlsxPaths)
        {
            try
            {
                if (xlsxPaths == null)
                    throw new ArgumentNullException(nameof(xlsxPaths));

                var workbookDatas = new List<NANINukoWorkbookData>();

                foreach (var path in xlsxPaths)
                {
                    if (string.IsNullOrWhiteSpace(path))
                        continue;

                    if (!File.Exists(path))
                        throw new FileNotFoundException("Excel file not found.", path);

                    workbookDatas.Add(NANINukoExcelLoader.Load(path));
                }

                return LoadFromWorkbookDatas(workbookDatas);
            }
            catch (Exception ex)
            {
                Debug.LogError("[NANINuko] LoadFromFiles failed: " + ex);
                Clear();
                return false;
            }
        }

        public static bool LoadFromWorkbookData(NANINukoWorkbookData workbookData)
        {
            return LoadFromWorkbookDatas(new[] { workbookData });
        }

        public static bool LoadFromWorkbookDatas(IEnumerable<NANINukoWorkbookData> workbookDatas)
        {
            try
            {
                if (workbookDatas == null)
                    throw new ArgumentNullException(nameof(workbookDatas));

                var db = new NANINukoRuntimeDatabase();
                db.Load(workbookDatas);

                if (!db.IsLoaded)
                {
                    Debug.LogError("[NANINuko] No workbook data loaded.");
                    Clear();
                    return false;
                }

                Clear();

                _database = db;
                NANINukoFlagStore.LoadDatabase(db);

                _lastTriggeredFrame = -1;

                Debug.Log(
                    $"[NANINuko] TriggerService loaded. flags={db.DefaultFlags.Count}, key={db.AllKeyEvents.Count}, zone={db.AllZoneEvents.Count}, pos={db.AllPositionEvents.Count}, chara={db.AllCharaEvents.Count}"
                );
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError("[NANINuko] LoadFromWorkbookDatas failed: " + ex);
                Clear();
                return false;
            }
        }

        public static void Clear()
        {
            _database = null;
            _lastTriggeredFrame = -1;
            NANINukoFlagStore.ClearAll();
        }

        public static void ResetFlagsToDefaults()
        {
            if (!IsLoaded)
                return;

            NANINukoFlagStore.ResetToDefaults();
        }

        public static bool TickKey()
        {
            return RunWithFrameLock(() =>
                NANINukoKeyTriggerService.Tick(_database)
            );
        }

        public static bool TryProcessZoneEnter(object chara, object zone)
        {
            return RunWithFrameLock(() =>
                NANINukoZoneTriggerService.TryProcessEnter(_database, chara, zone)
            );
        }

        public static bool TryProcessPosition(object pc, object zone)
        {
            return RunWithFrameLock(() =>
                NANINukoPositionTriggerService.TryProcessCurrent(_database, pc, zone)
            );
        }

        public static bool TickChara()
        {
            return RunWithFrameLock(() =>
                NANINukoCharaTriggerService.Tick(_database)
            );
        }

        public static bool IsLockedThisFrame()
        {
            return _lastTriggeredFrame == Time.frameCount;
        }

        private static bool RunWithFrameLock(Func<bool> action)
        {
            if (!IsLoaded)
                return false;

            if (action == null)
                return false;

            if (IsLockedThisFrame())
                return false;

            var triggered = false;

            try
            {
                triggered = action();
            }
            catch (Exception ex)
            {
                Debug.LogError("[NANINuko] Trigger action failed: " + ex);
                triggered = false;
            }

            if (triggered)
                _lastTriggeredFrame = Time.frameCount;

            return triggered;
        }
    }
}
