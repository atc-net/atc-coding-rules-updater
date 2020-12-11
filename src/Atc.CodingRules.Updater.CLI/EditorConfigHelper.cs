using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Atc.Data.Models;

namespace Atc.CodingRules.Updater.CLI
{
    public static class EditorConfigHelper
    {
        public static IEnumerable<LogKeyValueItem> Update(string rawCodingRulesDistribution, DirectoryInfo rootPath)
        {
            var logItems = new List<LogKeyValueItem>();

            logItems.AddRange(UpdateBuildProps(rawCodingRulesDistribution, new DirectoryInfo(Path.Combine(rootPath.FullName, "build"))));

            logItems.Add(UpdateEditorConfig("root", rawCodingRulesDistribution, rootPath, string.Empty));

            logItems.AddRange(UpdateEditorConfigAndDirectoryBuildFiles("src", rawCodingRulesDistribution, rootPath, "src", "src"));
            logItems.AddRange(UpdateEditorConfigAndDirectoryBuildFiles("test", rawCodingRulesDistribution, rootPath, "test", "test"));
            logItems.AddRange(UpdateEditorConfigAndDirectoryBuildFiles("sample", rawCodingRulesDistribution, rootPath, "sample", "sample"));

            return logItems;
        }

        private static IEnumerable<LogKeyValueItem> UpdateEditorConfigAndDirectoryBuildFiles(
            string area,
            string rawCodingRulesDistribution,
            DirectoryInfo rootPath,
            string filePart,
            string urlPart)
        {
            var path = new DirectoryInfo(Path.Combine(rootPath.FullName, filePart));
            var logItems = new List<LogKeyValueItem>
            {
                UpdateEditorConfig(area, rawCodingRulesDistribution, path, urlPart),
                UpdateDirectoryBuildProps(area, rawCodingRulesDistribution, path, urlPart),
                UpdateDirectoryBuildTargets(area, rawCodingRulesDistribution, path, urlPart),
            };

            return logItems;
        }

        private static IEnumerable<LogKeyValueItem> UpdateBuildProps(string rawCodingRulesDistribution, DirectoryInfo path)
        {
            try
            {
                if (!path.Exists)
                {
                    Directory.CreateDirectory(path.FullName);

                    var rawCommonPropsData = HttpClientHelper.GetRawFile($"{rawCodingRulesDistribution}/build/common.props");
                    File.WriteAllText(Path.Combine(path.FullName, "common.props"), rawCommonPropsData);

                    var rawCodeAnalysisPropsData = HttpClientHelper.GetRawFile($"{rawCodingRulesDistribution}/build/code-analysis.props");
                    File.WriteAllText(Path.Combine(path.FullName, "code-analysis.props"), rawCodeAnalysisPropsData);

                    return new List<LogKeyValueItem>
                    {
                        new LogKeyValueItem(LogCategoryType.Information, "FileUpdate", "common.props updated - Remember to change CompanyName in the file"),
                        new LogKeyValueItem(LogCategoryType.Debug, "FileUpdate", "code-analysis.props updated"),
                    };
                }
                else
                {
                    var file = new FileInfo(Path.Combine(path.FullName, "code-analysis.props"));
                    var rawCodeAnalysisPropsData = HttpClientHelper.GetRawFile($"{rawCodingRulesDistribution}/build/code-analysis.props");

                    if (!file.Exists)
                    {
                        File.WriteAllText(file.FullName, rawCodeAnalysisPropsData);

                        return new List<LogKeyValueItem>
                        {
                            new LogKeyValueItem(LogCategoryType.Debug, "FileUpdate", "code-analysis.props updated"),
                        };
                    }

                    var fileData = GetRawFile(file);

                    if (IsFileDataLengthEqual(rawCodeAnalysisPropsData, fileData))
                    {
                        return new List<LogKeyValueItem>
                        {
                            new LogKeyValueItem(LogCategoryType.Debug, "FileSkip", "code-analysis.props skipped"),
                        };
                    }

                    File.WriteAllText(file.FullName, rawCodeAnalysisPropsData);

                    return new List<LogKeyValueItem>
                    {
                        new LogKeyValueItem(LogCategoryType.Debug, "FileUpdate", "code-analysis.props updated"),
                    };
                }
            }
            catch (Exception ex)
            {
                return new List<LogKeyValueItem>
                {
                    new LogKeyValueItem(LogCategoryType.Error, "FileSkip", $"build folder skipped - {ex.Message}"),
                };
            }
        }

        private static LogKeyValueItem UpdateEditorConfig(string area, string rawCodingRulesDistribution, DirectoryInfo path, string urlPart)
        {
            var descriptionPart = string.IsNullOrEmpty(urlPart)
                ? ".editorconfig"
                : $"{urlPart}/.editorconfig";

            var file = new FileInfo(Path.Combine(path.FullName, ".editorconfig"));

            var rawGitUrl = string.IsNullOrEmpty(urlPart)
                ? $"{rawCodingRulesDistribution}/.editorconfig"
                : $"{rawCodingRulesDistribution}/{urlPart}/.editorconfig";

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
                    return new LogKeyValueItem(LogCategoryType.Debug, "FileSkip", $"{descriptionPart} skipped");
                }

                if (string.IsNullOrEmpty(rawFileData))
                {
                    File.WriteAllText(file.FullName, rawGitData);
                    return new LogKeyValueItem(LogCategoryType.Debug, "FileUpdate", $"{descriptionPart} updated");
                }

                var rawFileAtcData = GetRawFileAtcData(rawFileData);

                if (IsFileDataLengthEqual(rawGitData, rawFileAtcData))
                {
                    return new LogKeyValueItem(LogCategoryType.Debug, "FileSkip", $"{descriptionPart} skipped");
                }

                var rawFileCustomData = GetRawFileCustomData(rawFileData);
                var data = rawGitData + Environment.NewLine + rawFileCustomData;
                File.WriteAllText(file.FullName, data);

                return new LogKeyValueItem(LogCategoryType.Debug, "FileUpdate", $"{descriptionPart} files merged");
            }
            catch (Exception ex)
            {
                return new LogKeyValueItem(LogCategoryType.Error, "FileSkip", $"{area} skipped - {ex.Message}");
            }
        }

        private static LogKeyValueItem UpdateDirectoryBuildProps(string area, string rawCodingRulesDistribution, DirectoryInfo path, string urlPart)
        {
            var descriptionPart = string.IsNullOrEmpty(urlPart)
                ? "directory.build.props"
                : $"{urlPart}/directory.build.props";

            var file = new FileInfo(Path.Combine(path.FullName, "directory.build.props"));

            var rawGitUrl = string.IsNullOrEmpty(urlPart)
                ? $"{rawCodingRulesDistribution}/directory.build.props"
                : $"{rawCodingRulesDistribution}/{urlPart}/directory.build.props";

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
                    return new LogKeyValueItem(LogCategoryType.Debug, "FileSkip", $"{descriptionPart} skipped");
                }

                if (string.IsNullOrEmpty(rawFileData))
                {
                    File.WriteAllText(file.FullName, rawGitData);
                    return new LogKeyValueItem(LogCategoryType.Debug, "FileUpdate", $"{descriptionPart} updated");
                }

                var rawFileAtcData = GetRawFileAtcData(rawFileData);

                if (IsFileDataLengthEqual(rawGitData, rawFileAtcData))
                {
                    return new LogKeyValueItem(LogCategoryType.Debug, "FileSkip", $"{descriptionPart} skipped");
                }

                File.WriteAllText(file.FullName, rawGitData);

                return new LogKeyValueItem(LogCategoryType.Debug, "FileUpdate", $"{descriptionPart} updated");
            }
            catch (Exception ex)
            {
                return new LogKeyValueItem(LogCategoryType.Error, "FileSkip", $"{area} folder skipped - {ex.Message}");
            }
        }

        private static LogKeyValueItem UpdateDirectoryBuildTargets(string area, string rawCodingRulesDistribution, DirectoryInfo path, string urlPart)
        {
            var descriptionPart = string.IsNullOrEmpty(urlPart)
                ? "directory.build.targets"
                : $"{urlPart}/directory.build.targets";

            var file = new FileInfo(Path.Combine(path.FullName, "directory.build.targets"));

            var rawGitUrl = string.IsNullOrEmpty(urlPart)
                ? $"{rawCodingRulesDistribution}/directory.build.targets"
                : $"{rawCodingRulesDistribution}/{urlPart}/directory.build.targets";

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
                    return new LogKeyValueItem(LogCategoryType.Debug, "FileSkip", $"{descriptionPart} skipped");
                }

                if (string.IsNullOrEmpty(rawFileData))
                {
                    File.WriteAllText(file.FullName, rawGitData);
                    return new LogKeyValueItem(LogCategoryType.Debug, "FileUpdate", $"{descriptionPart} updated");
                }

                var rawFileAtcData = GetRawFileAtcData(rawFileData);

                if (IsFileDataLengthEqual(rawGitData, rawFileAtcData))
                {
                    return new LogKeyValueItem(LogCategoryType.Debug, "FileSkip", $"{descriptionPart} skipped");
                }

                File.WriteAllText(file.FullName, rawGitData);

                return new LogKeyValueItem(LogCategoryType.Debug, "FileSkip", $"{descriptionPart} updated");
            }
            catch (Exception ex)
            {
                return new LogKeyValueItem(LogCategoryType.Error, "FileSkip", $"{area} folder skipped - {ex.Message}");
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

        private static bool IsFileDataLengthEqual(string dataA, string dataB)
        {
            var l1 = dataA
                .Replace("\r\n", string.Empty, StringComparison.Ordinal)
                .Replace("\r", string.Empty, StringComparison.Ordinal)
                .Replace("\n", string.Empty, StringComparison.Ordinal)
                .Length;

            var l2 = dataB
                .Replace("\r\n", string.Empty, StringComparison.Ordinal)
                .Replace("\r", string.Empty, StringComparison.Ordinal)
                .Replace("\n", string.Empty, StringComparison.Ordinal)
                .Length;

            return l1.Equals(l2);
        }
    }
}