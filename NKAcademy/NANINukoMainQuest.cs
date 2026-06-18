using System;
using System.Collections.Generic;
using Cwl.API.Drama;
using UnityEngine;

namespace NANINuko
{
    public class NANINukoMainQuest : QuestSequence
    {
        public const int PhaseEntranceCeremony = 0;
        public const int PhaseRisei = 1;
        public const int PhasePurchase = 2;
        public const int PhaseGym = 3;
        public const int PhaseGarden = 4;
        public const int PhaseDorm = 5;
        public const int PhaseTask = 6;

        public const string QuestId = "academy_main";

        public override int RangeDeadLine => 0;
        public override bool CanAbandon => false;
        public override bool TrackOnStart => true;
        public override bool IsRandomQuest => false;

        public override bool RequireClientInSameZone => false;

        public override void OnInit()
        {
        }

        public override void OnStart()
        {
        }

        public override void OnChangePhase(int a)
        {
        }

        public override void OnComplete()
        {
        }

        public override bool UpdateOnTalk()
        {
            return false;
        }
    }

    public class NANINukoMainQuestCwlAddon : DramaOutcome
    {
        public static bool academy_main_quest_start(
            DramaManager dm,
            Dictionary<string, string> line,
            params string[] parameters)
        {
            try
            {
                if (EClass.game?.quests?.Get<NANINukoMainQuest>() != null)
                    return true;

                var c = EClass.game?.cards?.globalCharas?.Find("claris");
                var q = Quest.Create(NANINukoMainQuest.QuestId).SetClient(c, assignQuest: false);
                EClass.game.quests.Start(q);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINuko] academy_main_quest_start failed: " + ex);
                return false;
            }
        }

        public static bool academy_main_quest_next(
            DramaManager dm,
            Dictionary<string, string> line,
            params string[] parameters)
        {
            try
            {
                var quest = EClass.game?.quests?.Get<NANINukoMainQuest>();
                if (quest == null)
                {
                    Debug.LogWarning("[NANINuko] academy_main_quest_next: quest not found.");
                    return false;
                }

                quest.NextPhase();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINuko] academy_main_quest_next failed: " + ex);
                return false;
            }
        }

        public static bool academy_main_quest_set_phase(
            DramaManager dm,
            Dictionary<string, string> line,
            params string[] parameters)
        {
            try
            {
                if (!TryGetInt(line, parameters, out int phase))
                    return false;

                var quest = EClass.game?.quests?.Get<NANINukoMainQuest>();
                if (quest == null)
                {
                    Debug.LogWarning("[NANINuko] academy_main_quest_set_phase: quest not found.");
                    return false;
                }

                quest.ChangePhase(phase);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINuko] academy_main_quest_set_phase failed: " + ex);
                return false;
            }
        }

        public static bool academy_main_quest_complete(
            DramaManager dm,
            Dictionary<string, string> line,
            params string[] parameters)
        {
            try
            {
                var quest = EClass.game?.quests?.Get<NANINukoMainQuest>();
                if (quest == null)
                {
                    Debug.LogWarning("[NANINuko] academy_main_quest_complete: quest not found.");
                    return false;
                }

                EClass.game.quests.Complete(quest);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINuko] academy_main_quest_complete failed: " + ex);
                return false;
            }
        }

        private static bool TryGetInt(
            Dictionary<string, string> line,
            string[] parameters,
            out int value)
        {
            value = 0;
            string raw = string.Empty;

            if (parameters != null && parameters.Length > 0)
                raw = parameters[0];
            else if (line != null && line.TryGetValue("param", out var p))
                raw = p;

            raw = raw?.Trim() ?? string.Empty;
            return int.TryParse(raw, out value);
        }
    }
}
