using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
            OptionRoot options)
        {
            if (rootPath == null)
            {
                throw new ArgumentNullException(nameof(rootPath));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var logItems = new List<LogKeyValueItem>();

            if (HasOldBuildStructure(rootPath))
            {
                logItems.Add(
                    new LogKeyValueItem(
                        LogCategoryType.Warning,
                        "Old-Structure",
                        "Old structure has been detected - You have to manually drop 'directory.build.props' and re-create as 'Directory.Build.props' in GIT due too MsBuild issue on *nix."));
                logItems.AddRange(CleanupOldBuildStructure(rootPath));
            }

            var isFirstTime = IsFirstTime(rootPath);

            logItems.AddRange(EditorConfigHelper.HandleFile(isFirstTime: false, "root", rawCodingRulesDistribution, rootPath, string.Empty));
            logItems.AddRange(DirectoryBuildPropsHelper.HandleFile(isFirstTime: false, "root", rawCodingRulesDistribution, rootPath, string.Empty));

            if (options.HasMappingsPaths())
            {
                foreach (var item in options.Mappings.Sample.Paths)
                {
                    var path = new DirectoryInfo(item);
                    logItems.AddRange(EditorConfigHelper.HandleFile(isFirstTime, "sample", rawCodingRulesDistribution, path, "sample"));
                    logItems.AddRange(DirectoryBuildPropsHelper.HandleFile(isFirstTime, "sample", rawCodingRulesDistribution, path, "sample"));
                }

                foreach (var item in options.Mappings.Src.Paths)
                {
                    var path = new DirectoryInfo(item);
                    logItems.AddRange(EditorConfigHelper.HandleFile(isFirstTime, "src", rawCodingRulesDistribution, path, "src"));
                    logItems.AddRange(DirectoryBuildPropsHelper.HandleFile(isFirstTime, "src", rawCodingRulesDistribution, path, "src"));
                }

                foreach (var item in options.Mappings.Test.Paths)
                {
                    var path = new DirectoryInfo(item);
                    logItems.AddRange(EditorConfigHelper.HandleFile(isFirstTime, "test", rawCodingRulesDistribution, path, "test"));
                    logItems.AddRange(DirectoryBuildPropsHelper.HandleFile(isFirstTime, "test", rawCodingRulesDistribution, path, "test"));
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

        private static bool HasOldBuildStructure(DirectoryInfo rootPath)
        {
            var file = Path.Combine(rootPath.FullName, Path.Combine("build", "code-analysis.props"));
            return File.Exists(file);
        }

        [SuppressMessage("Design", "MA0051:Method is too long", Justification = "OK.")]
        private static IEnumerable<LogKeyValueItem> CleanupOldBuildStructure(DirectoryInfo rootPath)
        {
            var logItems = new List<LogKeyValueItem>();

            var file1 = Path.Combine(rootPath.FullName, Path.Combine("build", "code-analysis.props"));
            if (File.Exists(file1))
            {
                File.Delete(file1);
                logItems.Add(new LogKeyValueItem(LogCategoryType.Information, "FileDelete", "build/code-analysis.props"));
            }

            var file2 = Path.Combine(rootPath.FullName, Path.Combine("build", "common.props"));
            if (File.Exists(file2))
            {
                File.Delete(file2);
                logItems.Add(new LogKeyValueItem(LogCategoryType.Information, "FileDelete", "common.props"));
            }

            var dir1 = Path.Combine(rootPath.FullName, Path.Combine("build"));
            if (Directory.Exists(dir1) && Directory.GetFiles(dir1).Length == 0)
            {
                Directory.Delete(dir1);
                logItems.Add(new LogKeyValueItem(LogCategoryType.Information, "DirDelete", "build"));
            }

            var file3 = Path.Combine(rootPath.FullName, Path.Combine("sample", "directory.build.props"));
            if (File.Exists(file3))
            {
                File.Delete(file3);
                logItems.Add(new LogKeyValueItem(LogCategoryType.Information, "FileDelete", "sample/directory.build.props"));
            }

            var file4 = Path.Combine(rootPath.FullName, Path.Combine("sample", "directory.build.targets"));
            if (File.Exists(file4))
            {
                File.Delete(file4);
                logItems.Add(new LogKeyValueItem(LogCategoryType.Information, "FileDelete", "sample/directory.build.targets"));
            }

            var file5 = Path.Combine(rootPath.FullName, Path.Combine("src", "directory.build.props"));
            if (File.Exists(file5))
            {
                File.Delete(file5);
                logItems.Add(new LogKeyValueItem(LogCategoryType.Information, "FileDelete", "src/directory.build.props"));
            }

            var file6 = Path.Combine(rootPath.FullName, Path.Combine("src", "directory.build.targets"));
            if (File.Exists(file6))
            {
                File.Delete(file6);
                logItems.Add(new LogKeyValueItem(LogCategoryType.Information, "FileDelete", "src/directory.build.targets"));
            }

            var file7 = Path.Combine(rootPath.FullName, Path.Combine("test", "directory.build.props"));
            if (File.Exists(file7))
            {
                File.Delete(file7);
                logItems.Add(new LogKeyValueItem(LogCategoryType.Information, "FileDelete", "test/directory.build.props"));
            }

            var file8 = Path.Combine(rootPath.FullName, Path.Combine("test", "directory.build.targets"));
            if (File.Exists(file8))
            {
                File.Delete(file8);
                logItems.Add(new LogKeyValueItem(LogCategoryType.Information, "FileDelete", "test/directory.build.targets"));
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
            logItems.AddRange(DirectoryBuildPropsHelper.HandleFile(isFirstTime, area, rawCodingRulesDistribution, path, urlPart));
            return logItems;
        }
    }
}