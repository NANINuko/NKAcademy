using System;
using System.Collections.Generic;
using System.IO;
using Cwl.API.Drama;
using Newtonsoft.Json;
using UnityEngine;
using YKF;

namespace NANINuko.Lessons
{
    [Serializable]
    public sealed class NANINukoLessonDatabase
    {
        public NANINukoLessonDefinition[] lessons;

        public NANINukoLessonDefinition Find(int lessonId)
        {
            if (lessons == null)
                return null;

            for (int i = 0; i < lessons.Length; i++)
            {
                var lesson = lessons[i];
                if (lesson != null && lesson.lessonId == lessonId)
                    return lesson;
            }

            return null;
        }
    }

    [Serializable]
    public sealed class NANINukoLessonDefinition
    {
        public int lessonId;
        public string lessonName = "";
        public string rewardType = "skill_exp"; // "skill_exp" / "spell_stock"
        public int elementId;
        public int rewardValue = 1;
        public NANINukoLessonQuestion[] questions;

        public NANINukoLessonQuestion PickRandomQuestion()
        {
            if (questions == null || questions.Length == 0)
                return null;

            int totalWeight = 0;
            for (int i = 0; i < questions.Length; i++)
            {
                var q = questions[i];
                if (q == null)
                    continue;

                totalWeight += Mathf.Max(1, q.weight);
            }

            if (totalWeight <= 0)
                return null;

            int roll = UnityEngine.Random.Range(0, totalWeight);
            for (int i = 0; i < questions.Length; i++)
            {
                var q = questions[i];
                if (q == null)
                    continue;

                roll -= Mathf.Max(1, q.weight);
                if (roll < 0)
                    return q;
            }

            return questions[0];
        }
    }

    [Serializable]
    public sealed class NANINukoLessonQuestion
    {
        public string questionId = "";
        public string stepName = "";
        public int weight = 1;
    }

    public static class NANINukoLessonRuntime
    {
        public const string CurrentLessonFlag = "naninu.academy.current_lesson";
        public const string ProblemBookName = "問題集";

        private static string _jsonPath;
        private static NANINukoLessonDatabase _database;
        private static bool _loaded;

        private static NANINukoLessonDefinition _currentLesson;
        private static NANINukoLessonQuestion _currentQuestion;
        private static bool _rewardGranted;

        public static int CurrentLessonId { get; private set; } = -1;
        public static string CurrentQuestionId { get; private set; } = "";

        public static void Init(string jsonPath = null)
        {
            if (!string.IsNullOrWhiteSpace(jsonPath))
                _jsonPath = jsonPath.Trim();
        }

        public static bool Reload()
        {
            try
            {
                var path = string.IsNullOrWhiteSpace(_jsonPath) ? GetDefaultJsonPath() : _jsonPath;

                if (string.IsNullOrWhiteSpace(path))
                {
                    Debug.LogWarning("[NANINukoLesson] JSON path is empty.");
                    _loaded = false;
                    _database = null;
                    return false;
                }

                if (!File.Exists(path))
                {
                    Debug.LogWarning("[NANINukoLesson] JSON not found: " + path);
                    _loaded = false;
                    _database = null;
                    return false;
                }

                var json = File.ReadAllText(path);
                var db = JsonConvert.DeserializeObject<NANINukoLessonDatabase>(json);

                if (db == null)
                {
                    Debug.LogWarning("[NANINukoLesson] JSON parse failed: " + path);
                    _loaded = false;
                    _database = null;
                    return false;
                }

                _database = db;
                _loaded = true;

                Debug.Log("[NANINukoLesson] Loaded: " + path);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINukoLesson] Reload failed: " + ex);
                _loaded = false;
                _database = null;
                return false;
            }
        }

        public static bool SetCurrentLessonId(int lessonId)
        {
            if (EClass.player == null || EClass.player.dialogFlags == null)
                return false;

            EClass.player.dialogFlags[CurrentLessonFlag] = lessonId;
            return true;
        }

        public static bool TryOpenSelectedLesson()
        {
            if (!TryGetCurrentLessonId(out var lessonId))
                return false;

            return TryOpenLesson(lessonId);
        }

        public static bool TryOpenLesson(int lessonId)
        {
            if (!EnsureLoaded())
                return false;

            var lesson = _database.Find(lessonId);
            if (lesson == null)
            {
                Debug.LogWarning("[NANINukoLesson] Lesson not found: " + lessonId);
                return false;
            }

            var question = lesson.PickRandomQuestion();
            if (question == null || string.IsNullOrWhiteSpace(question.stepName))
            {
                Debug.LogWarning("[NANINukoLesson] Question not found or step empty. lesson=" + lessonId);
                return false;
            }

            var pc = EClass.pc;
            if (pc == null)
                return false;

            BeginRuntime(lesson, question);

            var prevForceJump = LayerDrama.forceJump;
            try
            {
                LayerDrama.forceJump = question.stepName.Trim();
                pc.ShowDialog(ProblemBookName);
            }
            finally
            {
                LayerDrama.forceJump = prevForceJump;
            }

            return true;
        }

        public static bool NotifyLessonCorrect()
        {
            if (_rewardGranted)
                return false;

            if (_currentLesson == null || _currentQuestion == null)
                return false;

            var pc = EClass.pc;
            if (pc == null || pc.elements == null)
                return false;

            try
            {
                if (_currentLesson.elementId <= 0)
                {
                    Debug.LogWarning("[NANINukoLesson] Invalid elementId: " + _currentLesson.elementId);
                    return false;
                }

                pc.elements.GetOrCreateElement(_currentLesson.elementId);

                var rewardType = NormalizeRewardType(_currentLesson.rewardType);
                switch (rewardType)
                {
                    case "spell_stock":
                        pc.elements.ModPotential(_currentLesson.elementId, _currentLesson.rewardValue);
                        break;

                    case "skill_exp":
                        pc.elements.ModExp(_currentLesson.elementId, _currentLesson.rewardValue);
                        break;

                    default:
                        Debug.LogWarning("[NANINukoLesson] Unknown rewardType: " + _currentLesson.rewardType);
                        return false;
                }

                _rewardGranted = true;
                Debug.Log($"[NANINukoLesson] Reward granted. lesson={_currentLesson.lessonId}, question={CurrentQuestionId}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINukoLesson] NotifyLessonCorrect failed: " + ex);
                return false;
            }
            finally
            {
                ClearRuntime();
            }
        }

        private static void BeginRuntime(NANINukoLessonDefinition lesson, NANINukoLessonQuestion question)
        {
            _currentLesson = lesson;
            _currentQuestion = question;
            CurrentLessonId = lesson != null ? lesson.lessonId : -1;
            CurrentQuestionId = question != null ? (question.questionId ?? "").Trim() : "";
            _rewardGranted = false;
        }

        private static void ClearRuntime()
        {
            _currentLesson = null;
            _currentQuestion = null;
            CurrentLessonId = -1;
            CurrentQuestionId = "";
            _rewardGranted = false;
        }

        private static bool EnsureLoaded()
        {
            if (_loaded && _database != null)
                return true;

            return Reload();
        }

        private static bool TryGetCurrentLessonId(out int lessonId)
        {
            lessonId = -1;

            var pc = EClass.player;
            if (pc == null || pc.dialogFlags == null)
                return false;

            return pc.dialogFlags.TryGetValue(CurrentLessonFlag, out lessonId);
        }

        private static string GetDefaultJsonPath()
        {
            try
            {
                var asm = typeof(NANINukoLessonRuntime).Assembly;
                var dir = Path.GetDirectoryName(asm.Location);

                if (string.IsNullOrWhiteSpace(dir))
                    dir = AppDomain.CurrentDomain.BaseDirectory;

                return Path.Combine(dir, "Lessons", "lessons.json");
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string NormalizeRewardType(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? string.Empty : text.Trim().ToLowerInvariant();
        }
    }

    public class NANINukoLessonCwlAddon : DramaOutcome
    {
        public static bool lesson_open_current(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
        {
            return NANINukoLessonRuntime.TryOpenSelectedLesson();
        }

        public static bool lesson_open(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
        {
            if (!TryGetIntArg(line, parameters, out var lessonId))
                return false;

            return NANINukoLessonRuntime.TryOpenLesson(lessonId);
        }

        public static bool lesson_set_current(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
        {
            if (!TryGetIntArg(line, parameters, out var lessonId))
                return false;

            return NANINukoLessonRuntime.SetCurrentLessonId(lessonId);
        }

        public static bool lesson_notify_correct(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
        {
            return NANINukoLessonRuntime.NotifyLessonCorrect();
        }

        public static bool lesson_reload(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
        {
            return NANINukoLessonRuntime.Reload();
        }

        private static bool TryGetIntArg(Dictionary<string, string> line, string[] parameters, out int value)
        {
            value = 0;
            string raw = "";

            if (parameters != null && parameters.Length > 0)
                raw = parameters[0];
            else if (line != null && line.TryGetValue("param", out var p))
                raw = p;

            raw = string.IsNullOrWhiteSpace(raw) ? "" : raw.Trim();
            return int.TryParse(raw, out value);
        }
    }
}