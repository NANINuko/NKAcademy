using System;
using System.Collections.Generic;
using System.IO;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using UnityEngine;
using NANINuko.Framework.Data;

namespace NANINuko.Framework.Excel
{
    public sealed class NANINukoWorkbookData
    {
        public readonly Dictionary<string, bool> DefaultFlags = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, string> FlagMemos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public readonly List<NANINukoEventDefinition> KeyEvents = new List<NANINukoEventDefinition>();
        public readonly List<NANINukoEventDefinition> ZoneEvents = new List<NANINukoEventDefinition>();
        public readonly List<NANINukoEventDefinition> PositionEvents = new List<NANINukoEventDefinition>();
        public readonly List<NANINukoEventDefinition> CharaEvents = new List<NANINukoEventDefinition>();
    }

    public static class NANINukoExcelLoader
    {
        public const int HeaderRowIndex = 0;

        public const string SheetFlags = "Flags";
        public const string SheetKeyEvents = "KeyEvents";
        public const string SheetZoneEvents = "ZoneEvents";
        public const string SheetPositionEvents = "PositionEvents";
        public const string SheetCharaEvents = "CharaEvents";

        public static NANINukoWorkbookData Load(string xlsxPath)
        {
            if (string.IsNullOrWhiteSpace(xlsxPath))
                throw new ArgumentException("xlsxPath is null or empty.", nameof(xlsxPath));

            if (!File.Exists(xlsxPath))
                throw new FileNotFoundException("Excel file not found.", xlsxPath);

            using (var stream = File.Open(xlsxPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var workbook = new XSSFWorkbook(stream);
                try
                {
                    return Load(workbook, xlsxPath);
                }
                finally
                {
                    (workbook as IDisposable)?.Dispose();
                }
            }
        }

        public static NANINukoWorkbookData Load(FileInfo file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            return Load(file.FullName);
        }

        public static NANINukoWorkbookData Load(IWorkbook workbook, string sourceName = "")
        {
            if (workbook == null)
                throw new ArgumentNullException(nameof(workbook));

            var data = new NANINukoWorkbookData();

            ReadFlagsSheet(workbook.GetSheet(SheetFlags), data, sourceName);
            ReadEventSheet(workbook.GetSheet(SheetKeyEvents), NANINukoTriggerType.Key, data.KeyEvents, sourceName);
            ReadEventSheet(workbook.GetSheet(SheetZoneEvents), NANINukoTriggerType.ZoneEnter, data.ZoneEvents, sourceName);
            ReadEventSheet(workbook.GetSheet(SheetPositionEvents), NANINukoTriggerType.Position, data.PositionEvents, sourceName);
            ReadEventSheet(workbook.GetSheet(SheetCharaEvents), NANINukoTriggerType.Chara, data.CharaEvents, sourceName);

            Debug.Log($"[NANINuko] Excel loaded: {sourceName}");
            Debug.Log($"[NANINuko] Flags={data.DefaultFlags.Count}, Key={data.KeyEvents.Count}, Zone={data.ZoneEvents.Count}, Pos={data.PositionEvents.Count}, Chara={data.CharaEvents.Count}");

            return data;
        }

        private static void ReadFlagsSheet(ISheet sheet, NANINukoWorkbookData data, string sourceName)
        {
            if (sheet == null)
                return;

            var formatter = new DataFormatter();
            var header = BuildHeaderMap(sheet, formatter);

            int colFlagId = GetColumnIndex(header, "FlagId", "Id", "Flag");
            int colDefault = GetOptionalColumnIndex(header, "Default", "Value", "DefaultValue");
            int colMemo = GetOptionalColumnIndex(header, "Memo", "Note", "Description");

            for (int r = HeaderRowIndex + 1; r <= sheet.LastRowNum; r++)
            {
                var row = sheet.GetRow(r);
                if (IsRowEmpty(row, formatter)) continue;

                var flagId = ReadString(row, colFlagId, formatter).Trim();
                if (string.IsNullOrWhiteSpace(flagId))
                    throw new InvalidDataException(BuildCellError(sourceName, sheet.SheetName, r, "FlagId is empty."));

                if (data.DefaultFlags.ContainsKey(flagId))
                    throw new InvalidDataException(BuildCellError(sourceName, sheet.SheetName, r, $"Duplicate FlagId: {flagId}"));

                bool defaultValue = colDefault >= 0 && !IsCellBlank(row.GetCell(colDefault), formatter)
                    ? ReadBool(row, colDefault, formatter, false)
                    : false;

                string memo = colMemo >= 0 ? ReadString(row, colMemo, formatter).Trim() : string.Empty;

                data.DefaultFlags.Add(flagId, defaultValue);
                if (!string.IsNullOrEmpty(memo))
                    data.FlagMemos[flagId] = memo;
            }
        }

        private static void ReadEventSheet(
            ISheet sheet,
            NANINukoTriggerType triggerType,
            List<NANINukoEventDefinition> output,
            string sourceName)
        {
            if (sheet == null)
                return;

            var formatter = new DataFormatter();
            var header = BuildHeaderMap(sheet, formatter);

            int colId = GetColumnIndex(header, "Id", "ID");
            int colEnabled = GetOptionalColumnIndex(header, "Enabled", "Enable");
            int colPreFlags = GetOptionalColumnIndex(header, "PreFlags", "PreFlag");
            int colDramaSheet = GetColumnIndex(header, "DramaSheet", "Drama");
            int colDramaStep = GetColumnIndex(header, "DramaStep", "Step");
            int colSetFlag = GetOptionalColumnIndex(header, "SetFlag", "PostFlag", "NextFlag");
            int colMemo = GetOptionalColumnIndex(header, "Memo", "Note", "Description");

            int colKey = -1;
            int colZoneId = -1;
            int colCharaId = -1;
            int colDetectType = -1;
            int colX1 = -1;
            int colX2 = -1;
            int colZ1 = -1;
            int colZ2 = -1;

            switch (triggerType)
            {
                case NANINukoTriggerType.Key:
                    colKey = GetColumnIndex(header, "Key", "KeyName");
                    break;

                case NANINukoTriggerType.ZoneEnter:
                    colZoneId = GetColumnIndex(header, "ZoneId", "ZoneID", "Zone");
                    break;

                case NANINukoTriggerType.Position:
                    colZoneId = GetColumnIndex(header, "ZoneId", "ZoneID", "Zone");
                    colX1 = GetColumnIndex(header, "X1", "XMin", "XStart");
                    colX2 = GetColumnIndex(header, "X2", "XMax", "XEnd");
                    colZ1 = GetColumnIndex(header, "Z1", "ZMin", "ZStart");
                    colZ2 = GetColumnIndex(header, "Z2", "ZMax", "ZEnd");
                    break;

                case NANINukoTriggerType.Chara:
                    colZoneId = GetColumnIndex(header, "ZoneId", "ZoneID", "Zone");
                    colCharaId = GetColumnIndex(header, "CharaId", "CharaID", "CharacterId", "CharacterID");
                    colDetectType = GetColumnIndex(header, "DetectType", "Detect");
                    break;
            }

            for (int r = HeaderRowIndex + 1; r <= sheet.LastRowNum; r++)
            {
                var row = sheet.GetRow(r);
                if (IsRowEmpty(row, formatter)) continue;

                var id = ReadString(row, colId, formatter).Trim();
                if (string.IsNullOrWhiteSpace(id))
                    throw new InvalidDataException(BuildCellError(sourceName, sheet.SheetName, r, "Id is empty."));

                var def = new NANINukoEventDefinition
                {
                    Id = id,
                    Enabled = colEnabled >= 0 ? ReadBool(row, colEnabled, formatter, true) : true,
                    TriggerType = triggerType,
                    PreFlags = colPreFlags >= 0 ? ReadString(row, colPreFlags, formatter).Trim() : string.Empty,
                    DramaSheet = ReadString(row, colDramaSheet, formatter).Trim(),
                    DramaStep = ReadString(row, colDramaStep, formatter).Trim(),
                    SetFlag = colSetFlag >= 0 ? ReadString(row, colSetFlag, formatter).Trim() : string.Empty,
                    Memo = colMemo >= 0 ? ReadString(row, colMemo, formatter).Trim() : string.Empty
                };

                if (string.IsNullOrWhiteSpace(def.DramaSheet))
                    throw new InvalidDataException(BuildCellError(sourceName, sheet.SheetName, r, "DramaSheet is empty."));
                if (string.IsNullOrWhiteSpace(def.DramaStep))
                    throw new InvalidDataException(BuildCellError(sourceName, sheet.SheetName, r, "DramaStep is empty."));

                switch (triggerType)
                {
                    case NANINukoTriggerType.Key:
                        def.KeyName = ReadString(row, colKey, formatter).Trim();
                        if (string.IsNullOrWhiteSpace(def.KeyName))
                            throw new InvalidDataException(BuildCellError(sourceName, sheet.SheetName, r, "Key is empty."));
                        break;

                    case NANINukoTriggerType.ZoneEnter:
                        def.ZoneId = ReadString(row, colZoneId, formatter).Trim();
                        if (string.IsNullOrWhiteSpace(def.ZoneId))
                            throw new InvalidDataException(BuildCellError(sourceName, sheet.SheetName, r, "ZoneId is empty."));
                        break;

                    case NANINukoTriggerType.Position:
                        def.ZoneId = ReadString(row, colZoneId, formatter).Trim();
                        if (string.IsNullOrWhiteSpace(def.ZoneId))
                            throw new InvalidDataException(BuildCellError(sourceName, sheet.SheetName, r, "ZoneId is empty."));

                        def.XMin = ReadInt(row, colX1, formatter);
                        def.XMax = ReadInt(row, colX2, formatter);
                        def.ZMin = ReadInt(row, colZ1, formatter);
                        def.ZMax = ReadInt(row, colZ2, formatter);

                        NormalizeRange(def);
                        break;

                    case NANINukoTriggerType.Chara:
                        def.ZoneId = ReadString(row, colZoneId, formatter).Trim();
                        if (string.IsNullOrWhiteSpace(def.ZoneId))
                            throw new InvalidDataException(BuildCellError(sourceName, sheet.SheetName, r, "ZoneId is empty."));

                        def.CharaId = ReadString(row, colCharaId, formatter).Trim();
                        if (string.IsNullOrWhiteSpace(def.CharaId))
                            throw new InvalidDataException(BuildCellError(sourceName, sheet.SheetName, r, "CharaId is empty."));

                        var detectTypeText = ReadString(row, colDetectType, formatter).Trim();
                        if (!TryParseDetectType(detectTypeText, out var detectType))
                            throw new InvalidDataException(BuildCellError(sourceName, sheet.SheetName, r, $"Invalid DetectType: {detectTypeText}"));
                        def.CharaDetectType = detectType;
                        break;
                }

                output.Add(def);
            }
        }

        private static bool TryParseDetectType(string text, out NANINukoCharaDetectType detectType)
        {
            if (Enum.TryParse(text, true, out detectType))
                return true;

            switch (text.Trim().ToLowerInvariant())
            {
                case "dead":
                case "death":
                case "死亡":
                    detectType = NANINukoCharaDetectType.Dead;
                    return true;

                case "missing":
                case "vanish":
                case "消滅":
                case "不在":
                    detectType = NANINukoCharaDetectType.Missing;
                    return true;
            }

            detectType = default(NANINukoCharaDetectType);
            return false;
        }

        private static void NormalizeRange(NANINukoEventDefinition def)
        {
            if (def.XMin > def.XMax)
                (def.XMin, def.XMax) = (def.XMax, def.XMin);

            if (def.ZMin > def.ZMax)
                (def.ZMin, def.ZMax) = (def.ZMax, def.ZMin);
        }

        private static Dictionary<string, int> BuildHeaderMap(ISheet sheet, DataFormatter formatter)
        {
            var headerRow = sheet.GetRow(HeaderRowIndex);
            if (headerRow == null)
                throw new InvalidDataException($"Header row not found: {sheet.SheetName}");

            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int c = headerRow.FirstCellNum; c < headerRow.LastCellNum; c++)
            {
                if (c < 0) continue;

                var name = GetCellText(headerRow.GetCell(c), formatter).Trim();
                if (string.IsNullOrWhiteSpace(name)) continue;

                if (map.ContainsKey(name))
                    throw new InvalidDataException($"Duplicate header name '{name}' in sheet '{sheet.SheetName}'.");

                map[name] = c;
            }

            return map;
        }

        private static int GetColumnIndex(Dictionary<string, int> headerMap, params string[] names)
        {
            foreach (var name in names)
            {
                if (headerMap.TryGetValue(name, out var index))
                    return index;
            }

            throw new InvalidDataException($"Required column not found: {string.Join(" / ", names)}");
        }

        private static int GetOptionalColumnIndex(Dictionary<string, int> headerMap, params string[] names)
        {
            foreach (var name in names)
            {
                if (headerMap.TryGetValue(name, out var index))
                    return index;
            }

            return -1;
        }

        private static string ReadString(IRow row, int columnIndex, DataFormatter formatter)
        {
            if (row == null || columnIndex < 0) return string.Empty;
            return GetCellText(row.GetCell(columnIndex), formatter);
        }

        private static int ReadInt(IRow row, int columnIndex, DataFormatter formatter)
        {
            var text = ReadString(row, columnIndex, formatter).Trim();
            if (int.TryParse(text, out var value))
                return value;

            throw new InvalidDataException($"Invalid integer value: '{text}'");
        }

        private static bool ReadBool(IRow row, int columnIndex, DataFormatter formatter, bool defaultValue)
        {
            var text = ReadString(row, columnIndex, formatter).Trim();

            if (string.IsNullOrWhiteSpace(text))
                return defaultValue;

            switch (text.ToLowerInvariant())
            {
                case "1":
                case "true":
                case "yes":
                case "on":
                case "y":
                case "t":
                case "ok":
                case "○":
                    return true;

                case "0":
                case "false":
                case "no":
                case "off":
                case "n":
                case "f":
                case "x":
                case "×":
                    return false;
            }

            throw new InvalidDataException($"Invalid bool value: '{text}'");
        }

        private static string GetCellText(ICell cell, DataFormatter formatter)
        {
            if (cell == null) return string.Empty;
            return formatter.FormatCellValue(cell) ?? string.Empty;
        }

        private static bool IsCellBlank(ICell cell, DataFormatter formatter)
        {
            if (cell == null) return true;
            return string.IsNullOrWhiteSpace(GetCellText(cell, formatter));
        }

        private static bool IsRowEmpty(IRow row, DataFormatter formatter)
        {
            if (row == null) return true;
            if (row.PhysicalNumberOfCells == 0) return true;

            for (int c = row.FirstCellNum; c < row.LastCellNum; c++)
            {
                if (c < 0) continue;
                if (!IsCellBlank(row.GetCell(c), formatter))
                    return false;
            }

            return true;
        }

        private static string BuildCellError(string sourceName, string sheetName, int rowIndex, string message)
        {
            return string.IsNullOrWhiteSpace(sourceName)
                ? $"[{sheetName}] row {rowIndex + 1}: {message}"
                : $"[{sourceName}::{sheetName}] row {rowIndex + 1}: {message}";
        }
    }
}
