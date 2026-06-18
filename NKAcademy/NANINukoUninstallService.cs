using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using NANINuko.Framework;
using NANINuko.Framework.Runtime;

namespace NANINuko
{
    public static class NANINukoUninstallService
    {
        private static readonly string[] AcademyZoneIds =
        {
            "chiirin_academy_normal",
            "chiirin_academy_dorm",
            "chiirin_academy_riverban"
        };

        public static bool Run()
        {
            try
            {
                if (EClass.game == null || EClass.player == null || EClass.pc == null)
                {
                    Debug.LogWarning("[NANINuko] Uninstall aborted: game state is not ready.");
                    return false;
                }

                Debug.Log("[NANINuko] Uninstall start");

                var workbookPath = NANINukoMainPlugin.GetWorkbookPath();
                if (!string.IsNullOrWhiteSpace(workbookPath))
                {
                    NANINukoFrameworkPlugin.UnregisterWorkbookPath(workbookPath, reloadIfLoaded: false);
                }

                var canRemoveZones = EnsurePlayerOutOfAcademyZone();

                if (NANINukoMainPlugin.Instance == null)
                {
                    Debug.LogWarning("[NANINuko] Plugin instance not found.");
                    return false;
                }

                NANINukoMainPlugin.Instance.StartCoroutine(
                    DelayedUninstall(canRemoveZones)
                );

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINuko] Uninstall failed: " + ex);
                return false;
            }
        }

        private static bool EnsurePlayerOutOfAcademyZone()
        {
            try
            {
                var pc = EClass.pc;
                if (pc == null)
                    return false;

                var currentZone = EClass._zone;
                if (!IsAcademyZone(currentZone))
                    return true;

                var safeZone = FindSafeZone(out var enterState);
                if (safeZone == null)
                {
                    Debug.LogWarning("[NANINuko] No safe zone found for retreat.");
                    return false;
                }

                if (IsAcademyZone(pc.homeZone))
                {
                    pc.homeZone = safeZone;
                }

                pc.MoveZone(safeZone, enterState);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINuko] EnsurePlayerOutOfAcademyZone failed: " + ex);
                return false;
            }
        }

        private static Zone FindSafeZone(out ZoneTransition.EnterState enterState)
        {
            enterState = ZoneTransition.EnterState.Return;

            try
            {
                var pc = EClass.pc;
                var game = EClass.game;

                var homeZone = pc?.homeZone;
                if (IsUsableSafeZone(homeZone))
                {
                    enterState = ZoneTransition.EnterState.Return;
                    return homeZone;
                }

                var startZone = game?.StartZone;
                if (IsUsableSafeZone(startZone))
                {
                    enterState = ZoneTransition.EnterState.Return;
                    return startZone;
                }

                var derphy = game?.spatials?.Find("derphy");
                if (IsUsableSafeZone(derphy))
                {
                    enterState = ZoneTransition.EnterState.Center;
                    return derphy;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINuko] FindSafeZone failed: " + ex);
            }

            return null;
        }

        private static bool IsUsableSafeZone(Zone zone)
        {
            if (zone == null)
                return false;

            return !IsAcademyZone(zone);
        }

        private static bool IsAcademyZone(Zone zone)
        {
            var zoneId = GetZoneId(zone);
            if (string.IsNullOrWhiteSpace(zoneId))
                return false;

            for (int i = 0; i < AcademyZoneIds.Length; i++)
            {
                if (string.Equals(zoneId, AcademyZoneIds[i], StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static string GetZoneId(Zone zone)
        {
            if (zone == null)
                return string.Empty;

            try
            {
                if (!string.IsNullOrWhiteSpace(zone.idExport))
                    return zone.idExport;

                if (!string.IsNullOrWhiteSpace(zone.id))
                    return zone.id;
            }
            catch
            {
            }

            return string.Empty;
        }

        private static void RemoveAcademyFlags()
        {
            try
            {
                var flags = EClass.player?.dialogFlags;
                if (flags != null)
                {
                    var keys = new List<string>(flags.Keys);
                    foreach (var key in keys)
                    {
                        if (string.IsNullOrWhiteSpace(key))
                            continue;

                        if (key.StartsWith("naninu.academy.", StringComparison.OrdinalIgnoreCase))
                        {
                            NANINukoFlagStore.RemoveFlag(key);
                        }
                    }
                }

                NANINukoFlagStore.RemoveFlag(NANINukoModeRuntime.ContentOnlyFlagId);
                NANINukoFlagStore.RemoveFlag(NANINukoModeRuntime.LegacyContentOnlyFlagId);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINuko] RemoveAcademyFlags failed: " + ex);
            }
        }

        private static void RemoveAcademyQuests()
        {
            try
            {
                var quests = EClass.game?.quests;
                if (quests == null)
                    return;

                RemoveQuest(
                    quests,
                    NANINukoAcademyGuildModule.GardenJoinQuestId,
                    typeof(QuestGuildGarden).ToString()
                );


                RemoveQuest(
                    quests,
                    NANINukoMainQuest.QuestId,
                    typeof(NANINukoMainQuest).ToString()
                );
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINuko] RemoveAcademyQuests failed: " + ex);
            }
        }

        private static void RemoveQuest(QuestManager quests, string questId, string questTypeName)
        {
            try
            {
                if (quests == null)
                    return;

                var q = quests.Get(questId) ?? quests.Get(questTypeName);
                if (q != null)
                {
                    DetachQuest(q);
                    quests.Remove(q);

                    quests.completedIDs.Remove(questId);
                    quests.completedIDs.Remove(q.id);
                    quests.completedTypes.Remove(questTypeName);
                    quests.completedTypes.Remove(q.GetType().ToString());
                }

                var g = quests.GetGlobal(questId) ?? quests.GetGlobal(questTypeName);
                if (g != null)
                {
                    DetachQuest(g);
                    quests.RemoveGlobal(g);

                    quests.completedIDs.Remove(questId);
                    quests.completedIDs.Remove(g.id);
                    quests.completedTypes.Remove(questTypeName);
                    quests.completedTypes.Remove(g.GetType().ToString());
                }

                quests.completedIDs.Remove(questId);
                quests.completedTypes.Remove(questTypeName);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINuko] RemoveQuest failed: " + ex);
            }
        }

        private static void DetachQuest(Quest q)
        {
            try
            {
                if (q == null)
                    return;

                if (q.task != null)
                {
                    q.task.owner = null;
                    q.task = null;
                }

                if (q.chara != null && q.chara.quest != null && q.chara.quest.uid == q.uid)
                {
                    q.chara.quest = null;
                }

                var zone = q.ClientZone;
                if (zone?.completedQuests != null)
                {
                    zone.completedQuests.Remove(q.uid);
                }
            }
            catch
            {
            }
        }

        private static void RemoveAcademyGuilds()
        {
            RemoveGuild<GuildGarden>(NANINukoAcademyGuildModule.GardenGuildId);
        }

        private static void RemoveGuild<T>(string guildId) where T : Guild
        {
            try
            {
                var factions = EClass.game?.factions;
                if (factions == null)
                    return;

                var guild = factions.Find<T>(guildId);
                if (guild == null)
                    return;

                try
                {
                    guild.relation.type = FactionRelation.RelationType.Default;
                    if (guild.relation.rank != 0)
                        guild.relation.rank = 0;
                    if (guild.relation.exp != 0)
                        guild.relation.exp = 0;
                    guild.relation.faction = null;
                }
                catch
                {
                }

                factions.dictAll.Remove(guild.id);
                factions.dictAll.Remove(guild.uid);

                Debug.Log("[NANINuko] Guild removed: " + guildId);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINuko] RemoveGuild failed: " + ex);
            }
        }

        private static bool RemoveAcademyZones()
        {
            try
            {
                var spatials = EClass.game?.spatials;
                if (spatials == null)
                    return false;

                bool allRemoved = true;

                foreach (var zoneId in AcademyZoneIds)
                {
                    if (string.IsNullOrWhiteSpace(zoneId))
                        continue;

                    var zone = spatials.Find(zoneId);
                    if (zone == null)
                        continue;

                    if (EClass._zone == zone)
                    {
                        Debug.LogWarning("[NANINuko] Still in academy zone, skipping: " + zoneId);
                        allRemoved = false;
                        continue;
                    }

                    try
                    {
                        zone.DeleteMapRecursive();
                    }
                    catch (Exception ex)
                    {
                        allRemoved = false;
                        Debug.LogWarning("[NANINuko] DeleteMapRecursive failed for " + zoneId + ": " + ex);
                    }

                    try
                    {
                        zone.Destroy();
                    }
                    catch (Exception ex)
                    {
                        allRemoved = false;
                        Debug.LogWarning("[NANINuko] Zone.Destroy failed for " + zoneId + ": " + ex);
                    }

                    Debug.Log("[NANINuko] Zone removed: " + zoneId);
                }

                return allRemoved;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINuko] RemoveAcademyZones failed: " + ex);
                return false;
            }
        }

        private static IEnumerator DelayedUninstall(bool canRemoveZones)
        {
            float timeout = 10f;

            while (IsAcademyZone(EClass._zone) && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            if (IsAcademyZone(EClass._zone))
            {
                Debug.LogWarning("[NANINuko] Retreat timeout. Zone removal skipped.");
                canRemoveZones = false;
            }
            else
            {
                yield return null;
            }

            RemoveAcademyFlags();
            RemoveAcademyQuests();
            RemoveAcademyGuilds();

            bool zonesRemoved = false;

            if (canRemoveZones)
            {
                zonesRemoved = RemoveAcademyZones();
            }
            else
            {
                Debug.LogWarning("[NANINuko] Academy zone removal skipped because retreat failed.");
            }

            if (!NANINukoModeRuntime.DeleteModeLockFile())
            {
                Debug.LogWarning("[NANINuko] Mode lock file delete failed.");
            }

            try
            {
                GameIO.SaveGame();
            }
            catch (Exception ex)
            {
                Debug.LogError("[NANINuko] SaveGame failed: " + ex);
            }

            if (zonesRemoved)
            {
                Dialog.Ok(
                    "アンインストール処理が完了しました。\n\n" +
                    "このあとタイトルに戻って、Mod一覧からこのModを無効化してください。",
                    null
                );
            }
            else
            {
                Dialog.Ok(
                    "アンインストール処理は一部完了しました。\n\n" +
                    "学園外への退避に失敗したため、ゾーン削除は保留です。\n" +
                    "学園外へ移動してから、もう一度アンインストールを実行してください。",
                    null
                );
            }

            Debug.Log("[NANINuko] Uninstall done");

            try
            {
                NANINukoFrameworkPlugin.ShutdownForUninstall();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINuko] Framework shutdown failed: " + ex);
            }

            NANINukoMainPlugin.Instance?.ShutdownForUninstall();
        }
    }
}
