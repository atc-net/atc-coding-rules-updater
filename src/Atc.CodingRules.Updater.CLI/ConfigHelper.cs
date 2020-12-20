using System;
using System.Collections.Generic;
using System.IO;
using Atc.CodingRules.Updater.CLI.Models;
using Atc.Data.Models;

namespace Atc.CodingRules.Updater.CLI
{
    public static class ConfigHelper
    {
        public static IEnumerable<LogKeyValueItem> HandleFiles(
            string rawCodingRulesDistribution,
            DirectoryInfo rootPath,
            Options options)
        {
            var logItems = new List<LogKeyValueItem>();

            var isFirstTime = IsFirstTime(rootPath);

            logItems.AddRange(HandleBuildPropsFiles(rawCodingRulesDistribution, new DirectoryInfo(Path.Combine(rootPath.FullName, "build"))));

            logItems.AddRange(EditorConfigHelper.HandleFile(isFirstTime: false, "root", rawCodingRulesDistribution, rootPath, string.Empty));

            if (options.HasMappingsPaths())
            {
                foreach (var item in options.Mappings.Sample.Paths)
                {
                    var path = new DirectoryInfo(item);
                    logItems.AddRange(EditorConfigHelper.HandleFile(isFirstTime, "sample", rawCodingRulesDistribution, path, "sample"));
                    logItems.Add(HandleDirectoryBuildPropsFile(isFirstTime, "sample", rawCodingRulesDistribution, path, "sample"));
                    logItems.Add(HandleDirectoryBuildTargetsFile(isFirstTime, "sample", rawCodingRulesDistribution, path, "sample"));
                }

                foreach (var item in options.Mappings.Src.Paths)
                {
                    var path = new DirectoryInfo(item);
                    logItems.AddRange(EditorConfigHelper.HandleFile(isFirstTime, "src", rawCodingRulesDistribution, path, "src"));
                    logItems.Add(HandleDirectoryBuildPropsFile(isFirstTime, "src", rawCodingRulesDistribution, path, "src"));
                    logItems.Add(HandleDirectoryBuildTargetsFile(isFirstTime, "src", rawCodingRulesDistribution, path, "src"));
                }

                foreach (var item in options.Mappings.Test.Paths)
                {
                    var path = new DirectoryInfo(item);
                    logItems.AddRange(EditorConfigHelper.HandleFile(isFirstTime, "test", rawCodingRulesDistribution, path, "test"));
                    logItems.Add(HandleDirectoryBuildPropsFile(isFirstTime, "test", rawCodingRulesDistribution, path, "test"));
                    logItems.Add(HandleDirectoryBuildTargetsFile(isFirstTime, "test", rawCodingRulesDistribution, path, "test"));
                }
            }
            else
            {
                logItems.AddRange(HandleEditorConfigAndDirectoryBuildFiles(isFirstTime, "sample", rawCodingRulesDistribution, rootPath, "sample", "sample"));
                logItems.AddRange(HandleEditorConfigAndDirectoryBuildFiles(isFirstTime, "src", rawCodingRulesDistribution, rootPath, "src", "src"));
                logItems.AddRange(HandleEditorConfigAndDirectoryBuildFiles(isFirstTime, "test", rawCodingRulesDistribution, rootPath, "test", "test"));
            }

            return logItems;
        }

        private static bool IsFirstTime(DirectoryInfo rootPath)
        {
            var file = new FileInfo(Path.Combine(rootPath.FullName, EditorConfigHelper.FileNameEditorConfig));
            return !file.Exists;
        }

        private static IEnumerable<LogKeyValueItem> HandleEditorConfigAndDirectoryBuildFiles(
            bool isFirstTime,
            string area,
            string rawCodingRulesDistribution,
            DirectoryInfo rootPath,
            string filePart,
            string urlPart)
        {
            var path = new DirectoryInfo(Path.Combine(rootPath.FullName, filePart));
            var logItems = new List<LogKeyValueItem>();
            logItems.AddRange(EditorConfigHelper.HandleFile(isFirstTime, area, rawCodingRulesDistribution, path, urlPart));
            logItems.Add(HandleDirectoryBuildPropsFile(isFirstTime, area, rawCodingRulesDistribution, path, urlPart));
            logItems.Add(HandleDirectoryBuildTargetsFile(isFirstTime, area, rawCodingRulesDistribution, path, urlPart));
            return logItems;
        }

        private static IEnumerable<LogKeyValueItem> HandleBuildPropsFiles(
            string rawCodingRulesDistribution,
            DirectoryInfo path)
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

                    var fileData = FileHelper.ReadAllText(file);

                    if (FileHelper.IsFileDataLengthEqual(rawCodeAnalysisPropsData, fileData))
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

        private static LogKeyValueItem HandleDirectoryBuildPropsFile(
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

            return HandleDirectoryBuildFile(isFirstTime, area, file, descriptionPart, rawGitUrl);
        }

        private static LogKeyValueItem HandleDirectoryBuildTargetsFile(
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

            return HandleDirectoryBuildFile(isFirstTime, area, file, descriptionPart, rawGitUrl);
        }

        private static LogKeyValueItem HandleDirectoryBuildFile(
            bool isFirstTime,
            string area,
            FileInfo file,
            string descriptionPart,
            string rawGitUrl)
        {
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
                var rawFileData = FileHelper.ReadAllText(file);

                if (FileHelper.IsFileDataLengthEqual(rawGitData, rawFileData))
                {
                    return new LogKeyValueItem(LogCategoryType.Debug, "FileSkip", $"{descriptionPart} skipped");
                }

                if (string.IsNullOrEmpty(rawFileData))
                {
                    File.WriteAllText(file.FullName, rawGitData);
                    return new LogKeyValueItem(LogCategoryType.Debug, "FileCreate", $"{descriptionPart} created");
                }

                File.WriteAllText(file.FullName, rawGitData);
                return new LogKeyValueItem(LogCategoryType.Debug, "FileUpdate", $"{descriptionPart} updated");
            }
            catch (Exception ex)
            {
                return new LogKeyValueItem(LogCategoryType.Error, "FileSkip", $"{area} folder skipped - {ex.Message}");
            }
        }
    }
}