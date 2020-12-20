using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Atc.Data.Models;

namespace Atc.CodingRules.Updater.CLI
{
    public static class EditorConfigHelper
    {
        public const string FileNameEditorConfig = ".editorconfig";

        public static IEnumerable<LogKeyValueItem> HandleFile(
            bool isFirstTime,
            string area,
            string rawCodingRulesDistribution,
            DirectoryInfo path,
            string urlPart)
        {
            var descriptionPart = string.IsNullOrEmpty(urlPart)
                ? FileNameEditorConfig
                : $"{urlPart}/{FileNameEditorConfig}";

            var file = new FileInfo(Path.Combine(path.FullName, FileNameEditorConfig));

            var rawGitUrl = string.IsNullOrEmpty(urlPart)
                ? $"{rawCodingRulesDistribution}/{FileNameEditorConfig}"
                : $"{rawCodingRulesDistribution}/{urlPart}/{FileNameEditorConfig}";

            var logItems = new List<LogKeyValueItem>();
            try
            {
                if (!file.Directory!.Exists)
                {
                    if (!isFirstTime)
                    {
                        logItems.Add(new LogKeyValueItem(LogCategoryType.Trace, "FileSkip", $"{descriptionPart} skipped"));
                        return logItems;
                    }

                    Directory.CreateDirectory(file.Directory.FullName);
                }

                var rawGitData = HttpClientHelper.GetRawFile(rawGitUrl);
                var rawFileData = FileHelper.ReadAllText(file);

                if (FileHelper.IsFileDataLengthEqual(rawGitData, rawFileData))
                {
                    logItems.Add(new LogKeyValueItem(LogCategoryType.Debug, "FileSkip", $"{descriptionPart} skipped"));
                    return logItems;
                }

                if (string.IsNullOrEmpty(rawFileData))
                {
                    return CreateFile(file, rawGitData, logItems, descriptionPart);
                }

                var rawFileAtcData = ExtractDataAndCutAfterCustomRulesHeader(rawFileData);

                if (FileHelper.IsFileDataLengthEqual(rawGitData, rawFileAtcData))
                {
                    logItems.Add(new LogKeyValueItem(LogCategoryType.Debug, "FileSkip", $"{descriptionPart} skipped"));
                    return logItems;
                }

                return UpdateFile(rawFileData, rawGitData, file, logItems, descriptionPart, rawFileAtcData);
            }
            catch (Exception ex)
            {
                logItems.Add(new LogKeyValueItem(LogCategoryType.Error, "FileSkip", $"{area} skipped - {ex.Message}"));
                return logItems;
            }
        }

        private static IEnumerable<LogKeyValueItem> CreateFile(
            FileInfo file,
            string rawGitData,
            List<LogKeyValueItem> logItems,
            string descriptionPart)
        {
            File.WriteAllText(file.FullName, rawGitData);
            logItems.Add(new LogKeyValueItem(LogCategoryType.Debug, "FileCreate", $"{descriptionPart} created"));
            return logItems;
        }

        private static IEnumerable<LogKeyValueItem> UpdateFile(
            string rawFileData,
            string rawGitData,
            FileInfo file,
            List<LogKeyValueItem> logItems,
            string descriptionPart,
            string rawFileAtcData)
        {
            var rawFileCustomData = ExtractCustomDataWithoutCustomRulesHeader(rawFileData);
            var data = rawGitData + Environment.NewLine + rawFileCustomData;

            File.WriteAllText(file.FullName, data);
            logItems.Add(new LogKeyValueItem(LogCategoryType.Debug, "FileUpdate", $"{descriptionPart} files merged"));

            var rawGitDataKeyValues = GetKeyValues(rawGitData);
            var rawFileDataKeyValues = GetKeyValues(rawFileAtcData);
            var rawFileCustomDataKeyValues = GetKeyValues(rawFileCustomData);
            logItems.AddRange(LogSeverityDiffs(rawGitDataKeyValues, rawFileDataKeyValues, rawFileCustomDataKeyValues));

            return logItems;
        }

        private static string ExtractDataAndCutAfterCustomRulesHeader(string rawFileData)
        {
            var lines = rawFileData.Split(FileHelper.LineBreaks, StringSplitOptions.None);
            var sb = new StringBuilder();

            foreach (var line in lines)
            {
                sb.AppendLine(line);
                if (!"# Custom - Code Analyzers Rules".Equals(line, StringComparison.Ordinal))
                {
                    continue;
                }

                sb.Append("##########################################");
                return sb.ToString();
            }

            return sb.ToString();
        }

        private static string ExtractCustomDataWithoutCustomRulesHeader(string rawFileData)
        {
            var lines = rawFileData.Split(FileHelper.LineBreaks, StringSplitOptions.None);
            var sb = new StringBuilder();
            var addLines = false;

            foreach (var line in lines)
            {
                if (addLines)
                {
                    if (!"##########################################".Equals(line, StringComparison.Ordinal))
                    {
                        sb.AppendLine(line);
                    }
                }
                else if ("# Custom - Code Analyzers Rules".Equals(line, StringComparison.Ordinal))
                {
                    addLines = true;
                }
            }

            return sb.ToString();
        }

        private static List<KeyValueItem> GetKeyValues(string data)
        {
            var list = new List<KeyValueItem>();

            var lines = data.Split(FileHelper.LineBreaks, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
                {
                    continue;
                }

                var keyValueLine = line.Split("=", StringSplitOptions.RemoveEmptyEntries);
                if (keyValueLine.Length == 2)
                {
                    list.Add(new KeyValueItem(keyValueLine[0], keyValueLine[1]));
                }
            }

            return list;
        }

        private static IEnumerable<LogKeyValueItem> LogSeverityDiffs(
            IEnumerable<KeyValueItem> rawGitDataKeyValues,
            IReadOnlyCollection<KeyValueItem> rawFileDataKeyValues,
            IReadOnlyCollection<KeyValueItem> rawFileCustomDataKeyValues)
        {
            var list = new List<LogKeyValueItem>();

            foreach (var rawGitDataKeyValue in rawGitDataKeyValues)
            {
                var key = rawGitDataKeyValue.Key;
                if (!key.StartsWith("dotnet_diagnostic.", StringComparison.Ordinal) ||
                    !key.Contains(".severity", StringComparison.Ordinal))
                {
                    continue;
                }

                if (rawFileCustomDataKeyValues.Any(x => x.Key.Equals(key, StringComparison.Ordinal)))
                {
                    // Duplicate
                    list.Add(new LogKeyValueItem(LogCategoryType.Warning, "- Duplicate key", key));
                }
                else if (!rawFileDataKeyValues.Any(x => x.Key.Equals(key, StringComparison.Ordinal)))
                {
                    // New
                    list.Add(new LogKeyValueItem(LogCategoryType.Debug, "- New key/value", $"{key}={rawGitDataKeyValue.Value}"));
                }
            }

            return list;
        }
    }
}