using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Atc.CodingRules.Updater.CLI.Models;
using Atc.Data.Models;

namespace Atc.CodingRules.Updater.CLI
{
    public static class EditorConfigHelper
    {
        private const string FileNameEditorConfig = ".editorconfig";

        public static IEnumerable<LogKeyValueItem> Update(
            string rawCodingRulesDistribution,
            DirectoryInfo rootPath,
            Options options)
        {
            var logItems = new List<LogKeyValueItem>();

            var isFirstTime = IsFirstTime(rootPath);

            logItems.AddRange(UpdateBuildProps(rawCodingRulesDistribution, new DirectoryInfo(Path.Combine(rootPath.FullName, "build"))));

            logItems.Add(UpdateEditorConfig(isFirstTime: false, "root", rawCodingRulesDistribution, rootPath, string.Empty));

            if (options.HasMappingsPaths())
            {
                foreach (var item in options.Mappings.Sample.Paths)
                {
                    var path = new DirectoryInfo(item);
                    logItems.Add(UpdateEditorConfig(isFirstTime, "sample", rawCodingRulesDistribution, path, "sample"));
                    logItems.Add(UpdateDirectoryBuildProps(isFirstTime, "sample", rawCodingRulesDistribution, path, "sample"));
                    logItems.Add(UpdateDirectoryBuildTargets(isFirstTime, "sample", rawCodingRulesDistribution, path, "sample"));
                }

                foreach (var item in options.Mappings.Src.Paths)
                {
                    var path = new DirectoryInfo(item);
                    logItems.Add(UpdateEditorConfig(isFirstTime, "src", rawCodingRulesDistribution, path, "src"));
                    logItems.Add(UpdateDirectoryBuildProps(isFirstTime, "src", rawCodingRulesDistribution, path, "src"));
                    logItems.Add(UpdateDirectoryBuildTargets(isFirstTime, "src", rawCodingRulesDistribution, path, "src"));
                }

                foreach (var item in options.Mappings.Test.Paths)
                {
                    var path = new DirectoryInfo(item);
                    logItems.Add(UpdateEditorConfig(isFirstTime, "test", rawCodingRulesDistribution, path, "test"));
                    logItems.Add(UpdateDirectoryBuildProps(isFirstTime, "test", rawCodingRulesDistribution, path, "test"));
                    logItems.Add(UpdateDirectoryBuildTargets(isFirstTime, "test", rawCodingRulesDistribution, path, "test"));
                }
            }
            else
            {
                logItems.AddRange(UpdateEditorConfigAndDirectoryBuildFiles(isFirstTime, "sample", rawCodingRulesDistribution, rootPath, "sample", "sample"));
                logItems.AddRange(UpdateEditorConfigAndDirectoryBuildFiles(isFirstTime, "src", rawCodingRulesDistribution, rootPath, "src", "src"));
                logItems.AddRange(UpdateEditorConfigAndDirectoryBuildFiles(isFirstTime, "test", rawCodingRulesDistribution, rootPath, "test", "test"));
            }

            return logItems;
        }

        private static bool IsFirstTime(DirectoryInfo rootPath)
        {
            var file = new FileInfo(Path.Combine(rootPath.FullName, FileNameEditorConfig));
            return !file.Exists;
        }

        private static IEnumerable<LogKeyValueItem> UpdateEditorConfigAndDirectoryBuildFiles(
            bool isFirstTime,
            string area,
            string rawCodingRulesDistribution,
            DirectoryInfo rootPath,
            string filePart,
            string urlPart)
        {
            var path = new DirectoryInfo(Path.Combine(rootPath.FullName, filePart));
            var logItems = new List<LogKeyValueItem>
            {
                UpdateEditorConfig(isFirstTime, area, rawCodingRulesDistribution, path, urlPart),
                UpdateDirectoryBuildProps(isFirstTime, area, rawCodingRulesDistribution, path, urlPart),
                UpdateDirectoryBuildTargets(isFirstTime, area, rawCodingRulesDistribution, path, urlPart),
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
                        new LogKeyValueItem(LogCategoryType.Information, "FileUpdate", "common.props updated - Remember to change the CompanyName in the file"),
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

        private static LogKeyValueItem UpdateEditorConfig(
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

            try
            {
                if (!file.Directory!.Exists)
                {
                    if (!isFirstTime)
                    {
                        return new LogKeyValueItem(LogCategoryType.Trace, "FileSkip", $"{descriptionPart} skipped");
                    }

                    Directory.CreateDirectory(file.Directory.FullName);
                }

                var rawGitData = HttpClientHelper.GetRawFile(rawGitUrl);
                var rawFileData = GetRawFile(file);

                if (IsFileDataLengthEqual(rawGitData, rawFileData))
                {
                    return new LogKeyValueItem(LogCategoryType.Debug, "FileSkip", $"{descriptionPart} skipped");
                }

                if (string.IsNullOrEmpty(rawFileData))
                {
                    File.WriteAllText(file.FullName, rawGitData);
                    return new LogKeyValueItem(LogCategoryType.Debug, "FileCreate", $"{descriptionPart} created");
                }

                var rawFileAtcData = GetRawFileAtcDataWithCustomRulesHeader(rawFileData);

                if (IsFileDataLengthEqual(rawGitData, rawFileAtcData))
                {
                    return new LogKeyValueItem(LogCategoryType.Debug, "FileSkip", $"{descriptionPart} skipped");
                }

                var rawFileCustomData = GetRawFileCustomDataWithoutCustomRulesHeader(rawFileData);
                var data = rawGitData + Environment.NewLine + rawFileCustomData;
                File.WriteAllText(file.FullName, data);

                return new LogKeyValueItem(LogCategoryType.Debug, "FileUpdate", $"{descriptionPart} files merged");
            }
            catch (Exception ex)
            {
                return new LogKeyValueItem(LogCategoryType.Error, "FileSkip", $"{area} skipped - {ex.Message}");
            }
        }

        private static LogKeyValueItem UpdateDirectoryBuildProps(
            bool isFirstTime,
            string area,
            string rawCodingRulesDistribution,
            DirectoryInfo path,
            string urlPart)
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
                    if (!isFirstTime)
                    {
                        return new LogKeyValueItem(LogCategoryType.Trace, "FileSkip", $"{descriptionPart} skipped");
                    }

                    Directory.CreateDirectory(file.Directory.FullName);
                }

                var rawGitData = HttpClientHelper.GetRawFile(rawGitUrl);
                var rawFileData = GetRawFile(file);

                if (IsFileDataLengthEqual(rawGitData, rawFileData))
                {
                    return new LogKeyValueItem(LogCategoryType.Debug, "FileSkip", $"{descriptionPart} skipped");
                }

                if (string.IsNullOrEmpty(rawFileData))
                {
                    File.WriteAllText(file.FullName, rawGitData);
                    return new LogKeyValueItem(LogCategoryType.Debug, "FileCreate", $"{descriptionPart} created");
                }

                var rawFileAtcData = GetRawFileAtcDataWithCustomRulesHeader(rawFileData);

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

        private static LogKeyValueItem UpdateDirectoryBuildTargets(
            bool isFirstTime,
            string area,
            string rawCodingRulesDistribution,
            DirectoryInfo path,
            string urlPart)
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
                    if (!isFirstTime)
                    {
                        return new LogKeyValueItem(LogCategoryType.Trace, "FileSkip", $"{descriptionPart} skipped");
                    }

                    Directory.CreateDirectory(file.Directory.FullName);
                }

                var rawGitData = HttpClientHelper.GetRawFile(rawGitUrl);
                var rawFileData = GetRawFile(file);

                if (IsFileDataLengthEqual(rawGitData, rawFileData))
                {
                    return new LogKeyValueItem(LogCategoryType.Debug, "FileSkip", $"{descriptionPart} skipped");
                }

                if (string.IsNullOrEmpty(rawFileData))
                {
                    File.WriteAllText(file.FullName, rawGitData);
                    return new LogKeyValueItem(LogCategoryType.Debug, "FileCreate", $"{descriptionPart} created");
                }

                var rawFileAtcData = GetRawFileAtcDataWithCustomRulesHeader(rawFileData);

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

        private static string GetRawFileAtcDataWithCustomRulesHeader(string rawFileData)
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

        private static string GetRawFileCustomDataWithoutCustomRulesHeader(string rawFileData)
        {
            var lines = rawFileData.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
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

        private static string GetRawFile(FileInfo file)
            => file.Exists
                ? File.ReadAllText(file.FullName)
                : string.Empty;

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