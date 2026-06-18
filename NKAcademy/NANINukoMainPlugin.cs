using System;
using System.IO;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using NANINuko.Framework;
using NANINuko.ElinWeather;

namespace NANINuko
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class NANINukoMainPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.naninuko.academy";
        public const string PluginName = "NANINuko Academy";
        public const string PluginVersion = "1.0.0";

        public static NANINukoMainPlugin Instance { get; private set; }

        private const string WorkbookFileName = "DramaTriggers/DramaTriggers.xlsx";

        private Harmony _harmony;
        private bool _gameReadyLogged;
        private bool _shutdownRequested;
        private bool _shutdownCompleted;

        private enum InitStage
        {
            WaitingCore,
            WaitingGame,
            WaitingPC,
            Ready
        }

        private InitStage _initStage = InitStage.WaitingCore;

        private void Awake()
        {
            try
            {
                Instance = this;

                Logger.LogInfo("[NANINuko] Plugin loading...");

                var mode = NANINukoModeRuntime.EnsureLoaded();
                Logger.LogInfo("[NANINuko] Mode: " + mode);

                if (mode == NANINukoMode.Off)
                {
                    Logger.LogInfo("[NANINuko] Mode=Off, skipping all features.");
                    return;
                }

                if (mode == NANINukoMode.ContentOnly)
                {
                    Logger.LogInfo("[NANINuko] ContentOnly mode: ClassCache registration only.");
                    return;
                }

                NANINukoConfig.Init(Config);

                RegisterWorkbookPathToFramework();

                _harmony = new Harmony(PluginGuid);
                _harmony.PatchAll();

                NANINukoNoonChimeModule.Init(_harmony);
                NANINukoAcademyGuildModule.Init(_harmony);

                Logger.LogInfo("[NANINuko] All modules initialized.");
            }
            catch (Exception ex)
            {
                Logger.LogError("[NANINuko] Awake failed: " + ex);
            }
        }

        private void Update()
        {
            if (_shutdownRequested)
                return;

            if (!NANINukoModeRuntime.IsFull)
                return;

            if (!EnsureGameReady())
                return;

            NANINukoWeatherController.Tick();
        }

        public void ShutdownForUninstall()
        {
            try
            {
                _shutdownRequested = true;
                enabled = false;
                ShutdownInternal();
            }
            catch (Exception ex)
            {
                Logger.LogWarning("[NANINuko] ShutdownForUninstall failed: " + ex);
            }
        }

        private void OnDestroy()
        {
            try
            {
                _shutdownRequested = true;
                ShutdownInternal();
            }
            catch (Exception ex)
            {
                Logger.LogWarning("[NANINuko] Cleanup failed: " + ex);
            }

            if (Instance == this)
                Instance = null;

            Logger.LogInfo("[NANINuko] Plugin unloaded.");
        }

        private void ShutdownInternal()
        {
            if (_shutdownCompleted)
                return;

            _shutdownCompleted = true;

            try
            {
                NANINukoConfig.Save();
            }
            catch (Exception ex)
            {
                Logger.LogWarning("[NANINuko] Config save failed: " + ex);
            }

            try
            {
                _harmony?.UnpatchSelf();
            }
            catch (Exception ex)
            {
                Logger.LogWarning("[NANINuko] Unpatch failed: " + ex);
            }
        }

        public static string GetWorkbookPath()
        {
            try
            {
                var baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrWhiteSpace(baseDir))
                    baseDir = AppDomain.CurrentDomain.BaseDirectory ?? string.Empty;

                return Path.Combine(baseDir, WorkbookFileName);
            }
            catch
            {
                return string.Empty;
            }
        }

        private void RegisterWorkbookPathToFramework()
        {
            try
            {
                var workbookPath = GetWorkbookPath();
                NANINukoFrameworkPlugin.RegisterWorkbookPath(workbookPath);

                Logger.LogInfo("[NANINuko] Workbook registered: " + workbookPath);
            }
            catch (Exception ex)
            {
                Logger.LogWarning("[NANINuko] Workbook registration failed: " + ex);
            }
        }

        private bool EnsureGameReady()
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
                    if (!_gameReadyLogged)
                    {
                        _gameReadyLogged = true;
                        Logger.LogInfo("[NANINuko] Game Ready");
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}