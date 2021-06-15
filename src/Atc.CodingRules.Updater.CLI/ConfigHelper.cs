using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Atc.CodingRules.AnalyzerProviders.Models;
using Atc.CodingRules.Updater.CLI.Models;
using Atc.Data.Models;

// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable InvertIf
namespace Atc.CodingRules.Updater.CLI
{
    public static class ConfigHelper
    {
        public static async Task<IEnumerable<LogKeyValueItem>> HandleFiles(
            string rawCodingRulesDistribution,
            DirectoryInfo rootPath,
            OptionRoot options,
            bool useTemporarySuppressions,
            DirectoryInfo? temporarySuppressionsPath)
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

            if (useTemporarySuppressions)
            {
                var analyzerProviderBaseRules = await AnalyzerProviderBaseRulesHelper.GetAnalyzerProviderBaseRules();
                var buildResult = DotnetBuildHelper.BuildAndCollectErrors(rootPath);
                var suppressionLinesPrAnalyzer = GetSuppressionLines(analyzerProviderBaseRules, buildResult);

                if (!suppressionLinesPrAnalyzer.Any())
                {
                    return logItems;
                }

                // TODO: Imp. this.
                if (temporarySuppressionsPath is null)
                {
                    var rootEditorConfigFile = new FileInfo(Path.Combine(rootPath.FullName, EditorConfigHelper.FileNameEditorConfig));

                    // TODO: Append to root/.editorconfig
                    // TODO: OWN SECTION IN THE BOTTOM..
                }
                else
                {
                    await CreateSuppressionsFileInTempPath(temporarySuppressionsPath, suppressionLinesPrAnalyzer);
                }
            }

            return logItems;
        }

        private static async Task CreateSuppressionsFileInTempPath(DirectoryInfo temporarySuppressionsPath, IEnumerable<Tuple<string, List<string>>> suppressionLinesPrAnalyzer)
        {
            var temporarySuppressionsFile = Path.Join(temporarySuppressionsPath.FullName, "AtcCodingRulesSuppressions.txt");

            var sb = new StringBuilder();
            foreach (var (analyzerName, suppressionLines) in suppressionLinesPrAnalyzer)
            {
                sb.Append(Environment.NewLine);
                sb.Append($"# {analyzerName}");
                sb.Append(Environment.NewLine);

                foreach (var suppressionLine in suppressionLines)
                {
                    sb.Append(suppressionLine);
                    sb.Append(Environment.NewLine);
                }
            }

            await using FileStream fs = File.Create(temporarySuppressionsFile);
            var content = new UTF8Encoding().GetBytes(sb.ToString());
            await fs.WriteAsync(content);
        }

        private static List<Tuple<string, List<string>>> GetSuppressionLines(IReadOnlyCollection<AnalyzerProviderBaseRuleData> analyzerProviderBaseRules, Dictionary<string, int> buildResult)
        {
            var suppressionLines = new List<Tuple<string, string>>();
            foreach (var (code, count) in buildResult.OrderBy(x => x.Key))
            {
                foreach (var analyzerProvider in analyzerProviderBaseRules)
                {
                    var rule = analyzerProvider.Rules.FirstOrDefault(x => x.Code.Equals(code, StringComparison.Ordinal));
                    if (rule is not null)
                    {
                        var suppressionLine = string.IsNullOrEmpty(rule.Category)
                            ? $"dotnet_diagnostic.{code}.severity = none\t\t\t\t# {count.Pluralize("occurrence")} - {rule.Title} - {rule.Link}"
                            : $"dotnet_diagnostic.{code}.severity = none\t\t\t\t# {count.Pluralize("occurrence")} - Category: '{rule.Category}' - {rule.Title} - {rule.Link}";
                        suppressionLines.Add(Tuple.Create(analyzerProvider.Name, suppressionLine));
                    }
                }
            }

            var groupedSuppressionLines = suppressionLines.GroupBy(item => item.Item1, StringComparer.Ordinal)
                .Select(group => new { AnalyzerName = group.Key, Values = group.Select(item => item.Item2).ToList() })
                .OrderBy(item => item.AnalyzerName).ToList();

            return groupedSuppressionLines.Select(item => Tuple.Create(item.AnalyzerName, item.Values)).ToList();
        }

        private static bool HasOldBuildStructure(DirectoryInfo rootPath)
        {
            var file = Path.Combine(rootPath.FullName, Path.Combine("build", "code-analysis.props"));
            return File.Exists(file);
        }

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

        /// <summary>
        /// Pluralize: takes a word, inserts a number in front, and makes the word plural if the number is not exactly 1.
        /// </summary>
        /// <example>"{n.Pluralize("maid")} a-milking.</example>
        /// <param name="number">The number of objects.</param>
        /// <param name="word">The word to make plural.</param>
        /// <param name="pluralSuffix">An optional suffix; "s" is the default.</param>
        /// <param name="singularSuffix">An optional suffix if the count is 1; "" is the default.</param>
        /// <returns>Formatted string: "number word[suffix]", pluralSuffix (default "s") only added if the number is not 1, otherwise singularSuffix (default "") added.</returns>
        private static string Pluralize(this int number, string word, string pluralSuffix = "s", string singularSuffix = "")
            => $@"{number} {word}{(number != 1 ? pluralSuffix : singularSuffix)}";
    }
}