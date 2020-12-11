using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Atc.Data.Models;

namespace Atc.CodingRules.Updater.CLI
{
    public static class EditorConfigHelper
    {
        public static List<LogKeyValueItem> Update(string rawCodingRulesDistribution, DirectoryInfo rootPath)
        {
            var logItems = new List<LogKeyValueItem>
            {
                Update("root", rawCodingRulesDistribution, rootPath, string.Empty),
                Update("src", rawCodingRulesDistribution, new DirectoryInfo(Path.Combine(rootPath.FullName, "src")), "src"),
                Update("test", rawCodingRulesDistribution, new DirectoryInfo(Path.Combine(rootPath.FullName, "test")), "test"),
                Update("sample", rawCodingRulesDistribution, new DirectoryInfo(Path.Combine(rootPath.FullName, "sample")), "sample"),
            };

            return logItems;
        }

        private static LogKeyValueItem Update(string area, string rawCodingRulesDistribution, DirectoryInfo path, string urlPart)
        {
            string logKey = string.IsNullOrEmpty(urlPart)
                ? $".editorconfig-{area}"
                : $"{urlPart.Replace("/", "_", StringComparison.Ordinal)}_.editorconfig-{area}";

            var file = new FileInfo(Path.Combine(path.FullName, ".editorconfig"));

            var rawGitUrl = string.IsNullOrEmpty(urlPart)
                ? $"{rawCodingRulesDistribution}/{urlPart}/.editorconfig"
                : $"{rawCodingRulesDistribution}/.editorconfig";

            try
            {
                if (!file.Directory!.Exists)
                {
                    Directory.CreateDirectory(file.Directory.FullName);
                }

                var rawGitData = HttpClientHelper.GetRawFile(rawGitUrl);
                var rawFileData = GetRawFile(file);

                if (rawGitData.Equals(rawFileData, StringComparison.Ordinal))
                {
                    return new LogKeyValueItem(LogCategoryType.Information, logKey, ".editorconfig skipped");
                }

                if (string.IsNullOrEmpty(rawFileData))
                {
                    File.WriteAllText(file.FullName, rawGitData);
                    return new LogKeyValueItem(LogCategoryType.Information, logKey, ".editorconfig updated");
                }

                var rawFileAtcData = GetRawFileAtcData(rawFileData);

                var rawGitDataLength = rawGitData
                    .Replace("\r\n", string.Empty, StringComparison.Ordinal)
                    .Replace("\r", string.Empty, StringComparison.Ordinal)
                    .Replace("\n", string.Empty, StringComparison.Ordinal);

                var rawFileAtcDataLength = rawFileAtcData
                    .Replace("\r\n", string.Empty, StringComparison.Ordinal)
                    .Replace("\r", string.Empty, StringComparison.Ordinal)
                    .Replace("\n", string.Empty, StringComparison.Ordinal);

                if (rawGitDataLength.Equals(rawFileAtcDataLength, StringComparison.Ordinal))
                {
                    return new LogKeyValueItem(LogCategoryType.Information, logKey, ".editorconfig skipped");
                }

                var rawFileCustomData = GetRawFileCustomData(rawFileData);
                var data = rawGitData + Environment.NewLine + rawFileCustomData;
                File.WriteAllText(file.FullName, data);

                return new LogKeyValueItem(LogCategoryType.Information, logKey, ".editorconfig updated with merge");
            }
            catch (Exception ex)
            {
                return new LogKeyValueItem(LogCategoryType.Error, logKey, $".editorconfig skipped - {ex.Message}");
            }
        }

        private static string GetRawFileAtcData(string rawFileData)
        {
            var lines = rawFileData.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var sb = new StringBuilder();

            foreach (var line in lines)
            {
                sb.AppendLine(line);
                if ("# Custom - Code Analyzers Rules".Equals(line, StringComparison.Ordinal))
                {
                    sb.Append("##########################################");
                    return sb.ToString();
                }
            }

            return sb.ToString();
        }

        private static string GetRawFileCustomData(string rawFileData)
        {
            var lines = rawFileData.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var sb = new StringBuilder();
            var addLines = false;

            foreach (var line in lines)
            {
                if (addLines)
                {
                    sb.AppendLine(line);
                }
                else if ("# Custom - Code Analyzers Rules".Equals(line, StringComparison.Ordinal))
                {
                    sb.AppendLine("##########################################");
                    sb.AppendLine(line);
                    addLines = true;
                }
            }

            return sb.ToString();
        }

        private static string GetRawFile(FileInfo file)
        {
            return file.Exists
                ? File.ReadAllText(file.FullName)
                : string.Empty;
        }
    }
}