using System;
using System.Collections.Generic;
using Cwl.API.Drama;
using HarmonyLib;
using UnityEngine;

namespace NANINuko
{
    public static class NANINukoAcademyGuildModule
    {
        public const string AcademyZoneId = "chiirin_academy_normal";
        public const string DepartmentFlag = "naninu.academy.department";

        public const string GardenGuildId = "guild_garden";
        public const string GardenJoinQuestId = "academy_garden_guild";

        private static bool _initialized;

        public static void Init(Harmony harmony)
        {
            if (_initialized)
                return;

            _initialized = true;

            Debug.Log("[NANINukoAcademyGuildModule] Initialized");
        }

        public static bool IsAcademyZone(Zone zone = null)
        {
            if (zone == null)
                zone = EClass._zone;

            return zone != null && string.Equals(zone.idExport, AcademyZoneId, StringComparison.OrdinalIgnoreCase);
        }

        public static string GetSelectedGuildId()
        {
            try
            {
                var flags = EClass.player?.dialogFlags;
                if (flags == null)
                    return string.Empty;

                if (flags.TryGetValue(DepartmentFlag, out int value) && value == 1)
                    return GardenGuildId;
            }
            catch
            {
            }

            return string.Empty;
        }

        public static void SetSelectedGuildId(string guildId)
        {
            try
            {
                if (EClass.player?.dialogFlags == null)
                    return;

                if (string.Equals(guildId, GardenGuildId, StringComparison.OrdinalIgnoreCase))
                    EClass.player.dialogFlags[DepartmentFlag] = 1;
                else
                    EClass.player.dialogFlags.Remove(DepartmentFlag);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINukoAcademyGuildModule] SetSelectedGuildId failed: " + ex);
            }
        }

        public static Guild GetSelectedGuild()
        {
            var id = GetSelectedGuildId();

            if (string.Equals(id, GardenGuildId, StringComparison.OrdinalIgnoreCase))
                return EClass.game?.factions?.Find<GuildGarden>(GardenGuildId);

            return null;
        }

        public static Guild ResolveGuildFromDramaTarget(Chara c)
        {
            try
            {
                if (c == null)
                    return null;

                var faction = c.Chara?.faction;
                if (faction is GuildGarden garden)
                    return garden;
            }
            catch
            {
            }

            return null;
        }

        public static Guild ResolveAcademyGuild(Chara dramaTarget = null)
        {
            var fromDrama = ResolveGuildFromDramaTarget(dramaTarget ?? LayerDrama.Instance?.drama?.tg?.chara);
            if (fromDrama != null)
            {
                SetSelectedGuildId(fromDrama.id);
                return fromDrama;
            }

            return GetSelectedGuild();
        }

        public static void ApplyMemberState(Guild guild)
        {
            try
            {
                if (guild == null)
                    return;

                guild.relation.type = FactionRelation.RelationType.Member;

                if (guild.relation.rank <= 0)
                    guild.relation.rank = 1;

                if (guild.relation.exp < 0)
                    guild.relation.exp = 0;

                guild.RefreshDevelopment();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINukoAcademyGuildModule] ApplyMemberState failed: " + ex);
            }
        }

        public static void AddHarvestContribution(int amount = 1)
        {
            try
            {
                var guild = EClass.game?.factions?.Find<GuildGarden>(GardenGuildId);
                if (guild == null)
                    return;

                if (guild.relation.type != FactionRelation.RelationType.Member)
                    return;

                guild.AddContribution(Math.Max(1, amount));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINukoAcademyGuildModule] AddHarvestContribution failed: " + ex);
            }
        }

        public static bool HasGardenRank5()
        {
            try
            {
                var guild = EClass.game?.factions?.Find<GuildGarden>(GardenGuildId);
                return guild != null
                    && guild.relation.type == FactionRelation.RelationType.Member
                    && guild.relation.rank >= 5;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsVegiOrFruit(Card card)
        {
            try
            {
                var cat = card?.category;
                return cat != null && (cat.IsChildOf("vegi") || cat.IsChildOf("fruit"));
            }
            catch
            {
                return false;
            }
        }
    }

    public class GuildGarden : Guild
    {
        public override QuestGuild Quest => EClass.game?.quests?.Get<QuestGuildGarden>();

        public override bool IsCurrentZone
        {
            get
            {
                return string.Equals(
                    NANINukoAcademyGuildModule.GetSelectedGuildId(),
                    NANINukoAcademyGuildModule.GardenGuildId,
                    StringComparison.OrdinalIgnoreCase
                );
            }
        }

        public int HarvestContributionBonus => 1 + relation.rank;
        public int HarvestSpeedBonus => 5 + relation.rank * 2;
    }

    public class QuestGuildGarden : QuestGuild
    {
        public override Guild guild => EClass.game?.factions?.Find<GuildGarden>(NANINukoAcademyGuildModule.GardenGuildId);

        public override void OnInit()
        {
        }
    }

    [HarmonyPatch(typeof(Guild), "get_Current")]
    internal static class Patch_Guild_Current
    {
        static void Postfix(ref Guild __result)
        {
            try
            {
                if (!NANINukoAcademyGuildModule.IsAcademyZone())
                    return;

                var resolved = NANINukoAcademyGuildModule.GetSelectedGuild();

                if (resolved != null)
                    __result = resolved;
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex);
            }
        }
    }

    [HarmonyPatch(typeof(Guild), "get_CurrentDrama")]
    internal static class Patch_Guild_CurrentDrama
    {
        static void Postfix(ref Guild __result)
        {
            try
            {
                var chara = LayerDrama.Instance?.drama?.tg?.chara;
                var resolved = NANINukoAcademyGuildModule.ResolveGuildFromDramaTarget(chara);

                if (resolved != null)
                {
                    NANINukoAcademyGuildModule.SetSelectedGuildId(resolved.id);
                    __result = resolved;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINuko] Guild.CurrentDrama patch failed: " + ex);
            }
        }
    }

    [HarmonyPatch(typeof(Guild), nameof(Guild.GetCurrentGuild))]
    internal static class Patch_Guild_GetCurrentGuild
    {
        static void Postfix(ref Guild __result)
        {
            try
            {
                if (!NANINukoAcademyGuildModule.IsAcademyZone())
                    return;

                var resolved = NANINukoAcademyGuildModule.GetSelectedGuild();

                if (resolved != null)
                    __result = resolved;
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex);
            }
        }
    }

    [HarmonyPatch(typeof(TaskHarvest), nameof(TaskHarvest.OnCreateProgress))]
    internal static class Patch_TaskHarvest_OnCreateProgress
    {
        static void Postfix(TaskHarvest __instance, Progress_Custom p)
        {
            try
            {
                if (__instance == null || p == null)
                    return;

                var prev = p.onProgressComplete;
                p.onProgressComplete = () =>
                {
                    prev?.Invoke();

                    if (__instance.owner != null && __instance.owner.IsPC && IsGardenWork(__instance))
                    {
                        NANINukoAcademyGuildModule.AddHarvestContribution(1);
                    }
                };
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINuko] TaskHarvest OnCreateProgress patch failed: " + ex);
            }
        }

        private static bool IsGardenWork(TaskHarvest task)
        {
            try
            {
                if (task == null)
                    return false;

                if (task.IsHarvest)
                    return true;

                if (task.pos != null && task.pos.IsFarmField)
                    return true;

                return false;
            }
            catch
            {
                return false;
            }
        }
    }

    [HarmonyPatch(typeof(Card), nameof(Card.GetPrice), new[] { typeof(CurrencyType), typeof(bool), typeof(PriceType), typeof(Chara) })]
    internal static class Patch_Card_GetPrice_GardenRank5
    {
        static void Postfix(Card __instance, CurrencyType currency, bool sell, PriceType priceType, Chara c, ref int __result)
        {
            try
            {
                if (__instance == null || __result <= 0)
                    return;

                if (!sell)
                    return;

                if (currency != CurrencyType.Money)
                    return;

                if ((uint)(priceType - 1) > 1u)
                    return;

                if (!NANINukoAcademyGuildModule.IsVegiOrFruit(__instance))
                    return;

                if (!NANINukoAcademyGuildModule.HasGardenRank5())
                    return;

                if (EClass.pc?.faith == EClass.game?.religions?.Harvest)
                    return;

                __result = Mathf.RoundToInt(__result * 1.5f);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINuko] Patch_Card_GetPrice_GardenRank5 failed: " + ex);
            }
        }
    }

    public class NANINukoAcademyGuildCwlAddon : DramaOutcome
    {
        public static bool academy_guild_select_garden(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
        {
            NANINukoAcademyGuildModule.SetSelectedGuildId(NANINukoAcademyGuildModule.GardenGuildId);
            return true;
        }

        public static bool academy_guild_start_garden(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
        {
            try
            {
                var c = LayerDrama.Instance?.drama?.tg?.chara ?? EClass.pc;
                if (c == null || EClass.game?.quests == null)
                    return false;

                NANINukoAcademyGuildModule.SetSelectedGuildId(NANINukoAcademyGuildModule.GardenGuildId);

                var factions = EClass.game?.factions;
                if (factions == null)
                    return false;

                var guild = factions.Find<GuildGarden>(NANINukoAcademyGuildModule.GardenGuildId);

                if (guild == null)
                {
                    guild = new GuildGarden();
                    guild.id = NANINukoAcademyGuildModule.GardenGuildId;
                    guild.Init();
                }

                NANINukoAcademyGuildModule.ApplyMemberState(guild);

                if (EClass.game.quests.Get<QuestGuildGarden>() == null)
                {
                    EClass.game.quests.Start(NANINukoAcademyGuildModule.GardenJoinQuestId, c);
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINuko] academy_guild_start_garden failed: " + ex);
                return false;
            }
        }

        public static bool academy_guild_promote(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
        {
            try
            {
                Guild.CurrentDrama.relation.Promote();
                Guild.GetCurrentGuild()?.RefreshDevelopment();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINuko] academy_guild_promote failed: " + ex);
                return false;
            }
        }
    }
}
