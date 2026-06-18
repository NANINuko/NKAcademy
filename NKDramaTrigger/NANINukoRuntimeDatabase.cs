using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NANINuko.Framework.Data;
using NANINuko.Framework.Excel;

namespace NANINuko.Framework.Runtime
{
    public sealed class NANINukoRuntimeDatabase
    {
        private readonly Dictionary<string, bool> _defaultFlags =
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, string> _flagMemos =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private readonly List<NANINukoEventDefinition> _allKeyEvents =
            new List<NANINukoEventDefinition>();

        private readonly List<NANINukoEventDefinition> _allZoneEvents =
            new List<NANINukoEventDefinition>();

        private readonly List<NANINukoEventDefinition> _allPositionEvents =
            new List<NANINukoEventDefinition>();

        private readonly List<NANINukoEventDefinition> _allCharaEvents =
            new List<NANINukoEventDefinition>();

        private readonly Dictionary<string, List<NANINukoEventDefinition>> _keyEventsByKey =
            new Dictionary<string, List<NANINukoEventDefinition>>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, List<NANINukoEventDefinition>> _zoneEventsByZone =
            new Dictionary<string, List<NANINukoEventDefinition>>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, List<NANINukoEventDefinition>> _positionEventsByZone =
            new Dictionary<string, List<NANINukoEventDefinition>>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, Dictionary<string, List<NANINukoEventDefinition>>> _charaEventsByZoneAndId =
            new Dictionary<string, Dictionary<string, List<NANINukoEventDefinition>>>(StringComparer.OrdinalIgnoreCase);

        public bool IsLoaded { get; private set; }

        public IReadOnlyDictionary<string, bool> DefaultFlags => _defaultFlags;
        public IReadOnlyDictionary<string, string> FlagMemos => _flagMemos;

        public IReadOnlyList<NANINukoEventDefinition> AllKeyEvents => _allKeyEvents.AsReadOnly();
        public IReadOnlyList<NANINukoEventDefinition> AllZoneEvents => _allZoneEvents.AsReadOnly();
        public IReadOnlyList<NANINukoEventDefinition> AllPositionEvents => _allPositionEvents.AsReadOnly();
        public IReadOnlyList<NANINukoEventDefinition> AllCharaEvents => _allCharaEvents.AsReadOnly();

        public void Load(NANINukoWorkbookData workbookData)
        {
            if (workbookData == null)
                throw new ArgumentNullException(nameof(workbookData));

            Load(new NANINukoWorkbookData[] { workbookData });
        }

        public void Load(IEnumerable<NANINukoWorkbookData> workbookDatas)
        {
            if (workbookDatas == null)
                throw new ArgumentNullException(nameof(workbookDatas));

            Clear();

            bool loadedAnyWorkbook = false;

            foreach (var workbookData in workbookDatas)
            {
                if (workbookData == null)
                    continue;

                loadedAnyWorkbook = true;

                foreach (var pair in workbookData.DefaultFlags)
                {
                    if (string.IsNullOrWhiteSpace(pair.Key))
                        continue;

                    _defaultFlags[pair.Key.Trim()] = pair.Value;
                }

                foreach (var pair in workbookData.FlagMemos)
                {
                    if (string.IsNullOrWhiteSpace(pair.Key))
                        continue;

                    _flagMemos[pair.Key.Trim()] = pair.Value ?? string.Empty;
                }

                AddAll(_allKeyEvents, workbookData.KeyEvents);
                AddAll(_allZoneEvents, workbookData.ZoneEvents);
                AddAll(_allPositionEvents, workbookData.PositionEvents);
                AddAll(_allCharaEvents, workbookData.CharaEvents);
            }

            if (!loadedAnyWorkbook)
            {
                IsLoaded = false;
                return;
            }

            IndexKeyEvents(_allKeyEvents);
            IndexZoneEvents(_allZoneEvents);
            IndexPositionEvents(_allPositionEvents);
            IndexCharaEvents(_allCharaEvents);

            IsLoaded = true;
        }

        public void Clear()
        {
            _defaultFlags.Clear();
            _flagMemos.Clear();

            _allKeyEvents.Clear();
            _allZoneEvents.Clear();
            _allPositionEvents.Clear();
            _allCharaEvents.Clear();

            _keyEventsByKey.Clear();
            _zoneEventsByZone.Clear();
            _positionEventsByZone.Clear();
            _charaEventsByZoneAndId.Clear();

            IsLoaded = false;
        }

        public Dictionary<string, bool> CreateInitialFlags()
        {
            return _defaultFlags.ToDictionary(
                pair => pair.Key,
                pair => pair.Value,
                StringComparer.OrdinalIgnoreCase
            );
        }

        public bool TryGetDefaultFlag(string flagId, out bool value)
        {
            if (string.IsNullOrWhiteSpace(flagId))
            {
                value = false;
                return false;
            }

            return _defaultFlags.TryGetValue(flagId.Trim(), out value);
        }

        public bool TryGetFlagMemo(string flagId, out string memo)
        {
            memo = string.Empty;

            if (string.IsNullOrWhiteSpace(flagId))
                return false;

            return _flagMemos.TryGetValue(flagId.Trim(), out memo);
        }

        public IReadOnlyList<NANINukoEventDefinition> GetKeyEvents(string keyName)
        {
            keyName = NormalizeKey(keyName);

            if (string.IsNullOrEmpty(keyName))
                return Array.Empty<NANINukoEventDefinition>();

            if (_keyEventsByKey.TryGetValue(keyName, out var list))
                return list.AsReadOnly();

            return Array.Empty<NANINukoEventDefinition>();
        }


        public IReadOnlyList<NANINukoEventDefinition> GetZoneEvents(string zoneId)
        {
            zoneId = NormalizeKey(zoneId);

            if (string.IsNullOrEmpty(zoneId))
                return Array.Empty<NANINukoEventDefinition>();

            if (_zoneEventsByZone.TryGetValue(zoneId, out var list))
                return list.AsReadOnly();

            return Array.Empty<NANINukoEventDefinition>();
        }


        public IReadOnlyList<NANINukoEventDefinition> GetPositionEvents(string zoneId)
        {
            zoneId = NormalizeKey(zoneId);

            if (string.IsNullOrEmpty(zoneId))
                return Array.Empty<NANINukoEventDefinition>();

            if (_positionEventsByZone.TryGetValue(zoneId, out var list))
                return list.AsReadOnly();

            return Array.Empty<NANINukoEventDefinition>();
        }


        public IReadOnlyList<NANINukoEventDefinition> GetCharaEvents(string zoneId, string charaId)
        {
            zoneId = NormalizeKey(zoneId);
            charaId = NormalizeKey(charaId);

            if (string.IsNullOrEmpty(zoneId) || string.IsNullOrEmpty(charaId))
                return Array.Empty<NANINukoEventDefinition>();

            if (!_charaEventsByZoneAndId.TryGetValue(zoneId, out var byChara))
                return Array.Empty<NANINukoEventDefinition>();

            if (byChara.TryGetValue(charaId, out var list))
                return list.AsReadOnly();

            return Array.Empty<NANINukoEventDefinition>();
        }


        private static void AddAll(List<NANINukoEventDefinition> target, IEnumerable<NANINukoEventDefinition> source)
        {
            if (source == null)
                return;

            foreach (var item in source)
            {
                if (item == null)
                    continue;

                target.Add(item);
            }
        }

        private void IndexKeyEvents(IEnumerable<NANINukoEventDefinition> events)
        {
            foreach (var ev in events)
            {
                var key = NormalizeKey(ev.KeyName);
                if (string.IsNullOrEmpty(key))
                    continue;

                AddIndexed(_keyEventsByKey, key, ev);
            }
        }

        private void IndexZoneEvents(IEnumerable<NANINukoEventDefinition> events)
        {
            foreach (var ev in events)
            {
                var zoneId = NormalizeKey(ev.ZoneId);
                if (string.IsNullOrEmpty(zoneId))
                    continue;

                AddIndexed(_zoneEventsByZone, zoneId, ev);
            }
        }

        private void IndexPositionEvents(IEnumerable<NANINukoEventDefinition> events)
        {
            foreach (var ev in events)
            {
                var zoneId = NormalizeKey(ev.ZoneId);
                if (string.IsNullOrEmpty(zoneId))
                    continue;

                AddIndexed(_positionEventsByZone, zoneId, ev);
            }
        }

        private void IndexCharaEvents(IEnumerable<NANINukoEventDefinition> events)
        {
            foreach (var ev in events)
            {
                var zoneId = NormalizeKey(ev.ZoneId);
                var charaId = NormalizeKey(ev.CharaId);

                if (string.IsNullOrEmpty(zoneId) || string.IsNullOrEmpty(charaId))
                    continue;

                if (!_charaEventsByZoneAndId.TryGetValue(zoneId, out var byChara))
                {
                    byChara = new Dictionary<string, List<NANINukoEventDefinition>>(StringComparer.OrdinalIgnoreCase);
                    _charaEventsByZoneAndId[zoneId] = byChara;
                }

                AddIndexed(byChara, charaId, ev);
            }
        }

        private static void AddIndexed(
            Dictionary<string, List<NANINukoEventDefinition>> index,
            string key,
            NANINukoEventDefinition ev)
        {
            if (!index.TryGetValue(key, out var list))
            {
                list = new List<NANINukoEventDefinition>();
                index[key] = list;
            }

            list.Add(ev);
        }

        private static string NormalizeKey(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? string.Empty : text.Trim();
        }
    }
}
