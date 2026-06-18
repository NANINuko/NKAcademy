using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace NANINuko.Framework.Runtime
{
    public static class NANINukoFlagStore
    {
        private static readonly Dictionary<string, int> _defaultFlags =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, int> _runtimeCache =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> _knownFlags =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> _warnedPreFlagTokens =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static bool IsLoaded { get; private set; }

        public static void LoadDatabase(NANINukoRuntimeDatabase database)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            _defaultFlags.Clear();
            _knownFlags.Clear();
            _runtimeCache.Clear();
            _warnedPreFlagTokens.Clear();

            foreach (var pair in database.DefaultFlags)
            {
                var key = NormalizeFlagId(pair.Key);
                if (string.IsNullOrEmpty(key))
                    continue;

                _defaultFlags[key] = pair.Value ? 1 : 0;
                _knownFlags.Add(key);
            }

            IsLoaded = true;

            Debug.Log($"[NANINuko] FlagStore loaded. defaults={_defaultFlags.Count}");
        }

        public static bool GetBool(string flagId)
        {
            return GetInt(flagId, 0) != 0;
        }

        public static int GetInt(string flagId, int defaultValue = 0)
        {
            flagId = NormalizeFlagId(flagId);
            if (string.IsNullOrEmpty(flagId))
                return defaultValue;

            if (_runtimeCache.TryGetValue(flagId, out var cached))
                return cached;

            if (TryReadGameFlag(flagId, out var gameValue))
            {
                _runtimeCache[flagId] = gameValue;
                return gameValue;
            }

            if (_defaultFlags.TryGetValue(flagId, out var defaultInt))
            {
                _runtimeCache[flagId] = defaultInt;
                return defaultInt;
            }

            return defaultValue;
        }

        public static bool HasFlag(string flagId)
        {
            return GetBool(flagId);
        }

        public static bool TryGetDefaultFlag(string flagId, out bool value)
        {
            flagId = NormalizeFlagId(flagId);
            if (string.IsNullOrEmpty(flagId))
            {
                value = false;
                return false;
            }

            if (_defaultFlags.TryGetValue(flagId, out var defaultValue))
            {
                value = defaultValue != 0;
                return true;
            }

            value = false;
            return false;
        }

        public static bool IsKnownFlag(string flagId)
        {
            flagId = NormalizeFlagId(flagId);
            return !string.IsNullOrEmpty(flagId) && _knownFlags.Contains(flagId);
        }

        public static void SetBool(string flagId, bool value)
        {
            SetInt(flagId, value ? 1 : 0);
        }

        public static void SetInt(string flagId, int value)
        {
            flagId = NormalizeFlagId(flagId);
            if (string.IsNullOrEmpty(flagId))
                return;

            _runtimeCache[flagId] = value;

            if (TryGetDialogFlagsDictionary(out var flags))
            {
                flags[flagId] = value;
            }

            _knownFlags.Add(flagId);
        }

        public static void RemoveFlag(string flagId)
        {
            flagId = NormalizeFlagId(flagId);
            if (string.IsNullOrEmpty(flagId))
                return;

            _runtimeCache.Remove(flagId);

            if (TryGetDialogFlagsDictionary(out var flags))
            {
                flags.Remove(flagId);
            }
        }

        public static bool MatchesPreFlags(string preFlags)
        {
            if (string.IsNullOrWhiteSpace(preFlags))
                return true;

            var tokens = preFlags.Split(',');
            foreach (var rawToken in tokens)
            {
                var token = NormalizeFlagId(rawToken);
                if (string.IsNullOrEmpty(token))
                    continue;

                if (!EvaluatePreFlagToken(token))
                    return false;
            }

            return true;
        }

        public static void ApplyDefaultsToGameIfPossible()
        {
            if (_defaultFlags.Count == 0)
                return;

            if (!TryGetDialogFlagsDictionary(out var flags))
                return;

            foreach (var pair in _defaultFlags)
            {
                if (!flags.ContainsKey(pair.Key))
                {
                    flags[pair.Key] = pair.Value;
                    _runtimeCache[pair.Key] = pair.Value;
                }
            }
        }

        public static void RefreshKnownFlagsFromGameIfPossible()
        {
            if (!TryGetDialogFlagsDictionary(out var flags))
                return;

            foreach (var key in _knownFlags)
            {
                if (flags.TryGetValue(key, out var raw))
                {
                    _runtimeCache[key] = raw;
                }
                else if (_defaultFlags.TryGetValue(key, out raw))
                {
                    _runtimeCache[key] = raw;
                }
            }
        }

        public static void ResetToDefaults()
        {
            foreach (var pair in _defaultFlags)
            {
                SetInt(pair.Key, pair.Value);
            }
        }

        public static void ClearAll()
        {
            _defaultFlags.Clear();
            _runtimeCache.Clear();
            _knownFlags.Clear();
            _warnedPreFlagTokens.Clear();

            IsLoaded = false;
        }

        public static void ClearCacheOnly()
        {
            _runtimeCache.Clear();
        }


        private static bool EvaluatePreFlagToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return true;

            token = token.Trim();

            if (token[0] == '!' && (token.Length == 1 || token[1] != '='))
            {
                var flag = NormalizeFlagId(token.Substring(1));
                if (string.IsNullOrEmpty(flag))
                {
                    WarnInvalidPreFlag(token, "missing flag name after '!'");
                    return false;
                }

                return !GetBool(flag);
            }

            string flagId;
            string op;
            string valueText;
            bool hasOperator;

            if (TryParseComparisonToken(token, out flagId, out op, out valueText, out hasOperator))
            {
                if (!TryParseComparisonValue(valueText, out var expectedValue))
                {
                    WarnInvalidPreFlag(token, "invalid comparison value");
                    return false;
                }

                int actualValue = GetInt(flagId, 0);
                return CompareValues(actualValue, expectedValue, op, token);
            }

            if (hasOperator)
            {
                WarnInvalidPreFlag(token, "invalid comparison syntax");
                return false;
            }

            return GetBool(token);
        }

        private static bool TryParseComparisonToken(
            string token,
            out string flagId,
            out string op,
            out string valueText,
            out bool hasOperator)
        {
            flagId = string.Empty;
            op = string.Empty;
            valueText = string.Empty;
            hasOperator = false;

            if (string.IsNullOrWhiteSpace(token))
                return false;

            if (!FindComparisonOperator(token, out var opIndex, out var opLength, out var foundOp))
                return false;

            hasOperator = true;
            op = foundOp;
            flagId = NormalizeFlagId(token.Substring(0, opIndex));
            valueText = NormalizeFlagId(token.Substring(opIndex + opLength));

            return !string.IsNullOrEmpty(flagId) && !string.IsNullOrEmpty(valueText);
        }

        private static bool FindComparisonOperator(
            string token,
            out int opIndex,
            out int opLength,
            out string op)
        {
            opIndex = -1;
            opLength = 0;
            op = string.Empty;

            for (int i = 0; i < token.Length; i++)
            {
                char c = token[i];

                if (c == '>' || c == '<' || c == '=' || c == '!')
                {
                    if (i + 1 < token.Length)
                    {
                        char next = token[i + 1];

                        if (c == '>' && next == '=')
                        {
                            opIndex = i;
                            opLength = 2;
                            op = ">=";
                            return true;
                        }

                        if (c == '<' && next == '=')
                        {
                            opIndex = i;
                            opLength = 2;
                            op = "<=";
                            return true;
                        }

                        if (c == '=' && next == '=')
                        {
                            opIndex = i;
                            opLength = 2;
                            op = "==";
                            return true;
                        }

                        if (c == '!' && next == '=')
                        {
                            opIndex = i;
                            opLength = 2;
                            op = "!=";
                            return true;
                        }
                    }

                    if (c == '>' || c == '<' || c == '=')
                    {
                        opIndex = i;
                        opLength = 1;
                        op = c.ToString();
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryParseComparisonValue(string text, out int value)
        {
            text = NormalizeFlagId(text);
            if (string.IsNullOrEmpty(text))
            {
                value = 0;
                return false;
            }

            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                return true;

            if (TryParseBoolLike(text, out var boolValue))
            {
                value = boolValue ? 1 : 0;
                return true;
            }

            value = 0;
            return false;
        }

        private static bool TryParseBoolLike(string text, out bool value)
        {
            if (bool.TryParse(text, out value))
                return true;

            switch (text.Trim().ToLowerInvariant())
            {
                case "1":
                case "true":
                case "yes":
                case "on":
                case "y":
                case "t":
                case "ok":
                case "○":
                    value = true;
                    return true;

                case "0":
                case "false":
                case "no":
                case "off":
                case "n":
                case "f":
                case "x":
                case "×":
                    value = false;
                    return true;
            }

            value = false;
            return false;
        }

        private static bool CompareValues(int actual, int expected, string op, string token)
        {
            switch (op)
            {
                case "=":
                case "==":
                    return actual == expected;

                case "!=":
                    return actual != expected;

                case ">":
                    return actual > expected;

                case "<":
                    return actual < expected;

                case ">=":
                    return actual >= expected;

                case "<=":
                    return actual <= expected;

                default:
                    WarnInvalidPreFlag(token, "unknown operator");
                    return false;
            }
        }

        private static void WarnInvalidPreFlag(string token, string reason)
        {
            var warnKey = token + "||" + reason;
            if (!_warnedPreFlagTokens.Add(warnKey))
                return;

            Debug.LogWarning("[NANINuko] Invalid PreFlags token: '" + token + "' (" + reason + ")");
        }

        private static bool TryReadGameFlag(string flagId, out int value)
        {
            value = 0;

            if (!TryGetDialogFlagsDictionary(out var flags))
                return false;

            return flags.TryGetValue(flagId, out value);
        }

        private static bool TryGetDialogFlagsDictionary(out Dictionary<string, int> flags)
        {
            flags = EClass.player?.dialogFlags;
            return flags != null;
        }

        private static string NormalizeFlagId(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? string.Empty : text.Trim();
        }
    }
}
