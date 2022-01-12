using System.Drawing;
using Atc.DotNet;
using Microsoft.Extensions.Logging.Abstractions;
using OfficeOpenXml;
using OfficeOpenXml.Style;

// ReSharper disable InvertIf
// ReSharper disable SuggestBaseTypeForParameter
namespace Atc.CodingRules.Updater.CLI;

public static class ProjectHelper
{
    // TODO: Use this "multi-distributions" branch for now.
    private const string RawCodingRulesDistributionBaseUrl = "https://raw.githubusercontent.com/atc-net/atc-coding-rules/feature/multi-distributions/distribution";
    ////private const string RawCodingRulesDistributionBaseUrl = "https://raw.githubusercontent.com/atc-net/atc-coding-rules/main/distribution";
    private const string AtcCodingRulesSuppressionsFileName = "AtcCodingRulesSuppressions.txt";

    private const string AtcCodingRulesSuppressionsFileNameAsExcel = "AtcCodingRulesSuppressions.xlsx";
    private const int MaxNumberOfTimesToBuild = 9;
    private const int BuildDefaultTimeoutInSec = 1200;

    public static async Task HandleFiles(
        ILogger logger,
        DirectoryInfo projectPath,
        Options options)
    {
        ArgumentNullException.ThrowIfNull(projectPath);
        ArgumentNullException.ThrowIfNull(options);

        HandleEditorConfigFiles(logger, projectPath, options);

        if (options.ProjectTarget
            is SupportedProjectTargetType.DotNetCore
            or SupportedProjectTargetType.DotNet5
            or SupportedProjectTargetType.DotNet6)
        {
            HandleDirectoryBuildPropsFiles(logger, projectPath, options);

            if (options.UseTemporarySuppressions)
            {
                DirectoryInfo? temporarySuppressionsPath = null;
                if (!string.IsNullOrEmpty(options.TemporarySuppressionsPath))
                {
                    temporarySuppressionsPath = new DirectoryInfo(options.TemporarySuppressionsPath);
                }

                FileInfo? buildFile = null;
                if (!string.IsNullOrEmpty(options.BuildFile))
                {
                    buildFile = new FileInfo(options.BuildFile);
                }

                SanityCheckConflictingOptionsInCsprojFiles(throwIf: true, NullLogger.Instance, projectPath, options);

                await HandleTemporarySuppressions(
                    logger,
                    projectPath,
                    buildFile,
                    temporarySuppressionsPath,
                    options.TemporarySuppressionAsExcel);
            }
        }
    }

    public static Task SanityCheckFiles(
        ILogger logger,
        DirectoryInfo projectPath,
        Options options)
    {
        ArgumentNullException.ThrowIfNull(projectPath);
        ArgumentNullException.ThrowIfNull(options);

        SanityCheckConflictingOptionsInCsprojFiles(throwIf: false, logger, projectPath, options);

        return Task.Delay(1);
    }

    private static void SanityCheckConflictingOptionsInCsprojFiles(
        bool throwIf,
        ILogger logger,
        DirectoryInfo projectPath,
        Options options)
    {
        if (options.ProjectTarget == SupportedProjectTargetType.DotNet5)
        {
            var fileWithElementEnableNETAnalyzers = DotnetCsProjHelper.SearchAllForElement(projectPath, "EnableNETAnalyzers", "true");
            if (fileWithElementEnableNETAnalyzers.Any())
            {
                var sb = new StringBuilder();
                const string header = "EnableNETAnalyzers in .csproj causes build errors, please remove the element from the following files:";
                if (throwIf)
                {
                    sb.AppendLine(header);
                }
                else
                {
                    logger.LogWarning(header);
                }

                foreach (var file in fileWithElementEnableNETAnalyzers)
                {
                    if (throwIf)
                    {
                        sb.AppendLine(5, file.FullName);
                    }
                    else
                    {
                        logger.LogWarning($"     {file.FullName}");
                    }
                }

                if (throwIf)
                {
                    throw new DataException(sb.ToString());
                }
            }
        }
    }

    private static void HandleEditorConfigFiles(
        ILogger logger,
        DirectoryInfo projectPath,
        Options options)
    {
        logger.LogInformation($"{EmojisConstants.AreaEditorConfig} Working on EditorConfig files");

        var rawCodingRulesDistributionProjectTargetBaseUrl = $"{RawCodingRulesDistributionBaseUrl}/{options.ProjectTarget.ToStringLowerCase()}";
        EditorConfigHelper.HandleFile(logger, "root", rawCodingRulesDistributionProjectTargetBaseUrl, projectPath, string.Empty);

        foreach (var item in options.Mappings.Sample.Paths)
        {
            var path = new DirectoryInfo(item);
            EditorConfigHelper.HandleFile(logger, "sample", rawCodingRulesDistributionProjectTargetBaseUrl, path, "sample");
        }

        foreach (var item in options.Mappings.Src.Paths)
        {
            var path = new DirectoryInfo(item);
            EditorConfigHelper.HandleFile(logger, "src", rawCodingRulesDistributionProjectTargetBaseUrl, path, "src");
        }

        foreach (var item in options.Mappings.Test.Paths)
        {
            var path = new DirectoryInfo(item);
            EditorConfigHelper.HandleFile(logger, "test", rawCodingRulesDistributionProjectTargetBaseUrl, path, "test");
        }
    }

    private static void HandleDirectoryBuildPropsFiles(
        ILogger logger,
        DirectoryInfo projectPath,
        Options options)
    {
        logger.LogInformation($"{EmojisConstants.AreaDirectoryBuildProps} Working on Directory.Build.props files");
        var rawCodingRulesDistributionProjectTargetBaseUrl = $"{RawCodingRulesDistributionBaseUrl}/{options.ProjectTarget.ToStringLowerCase()}";

        DirectoryBuildPropsHelper.HandleFile(logger, "root", rawCodingRulesDistributionProjectTargetBaseUrl, options.UseLatestMinorNugetVersion, projectPath, string.Empty);

        foreach (var item in options.Mappings.Sample.Paths)
        {
            var path = new DirectoryInfo(item);
            DirectoryBuildPropsHelper.HandleFile(logger, "sample", rawCodingRulesDistributionProjectTargetBaseUrl, options.UseLatestMinorNugetVersion, path, "sample");
        }

        foreach (var item in options.Mappings.Src.Paths)
        {
            var path = new DirectoryInfo(item);
            DirectoryBuildPropsHelper.HandleFile(logger, "src", rawCodingRulesDistributionProjectTargetBaseUrl, options.UseLatestMinorNugetVersion, path, "src");
        }

        foreach (var item in options.Mappings.Test.Paths)
        {
            var path = new DirectoryInfo(item);
            DirectoryBuildPropsHelper.HandleFile(logger, "test", rawCodingRulesDistributionProjectTargetBaseUrl, options.UseLatestMinorNugetVersion, path, "test");
        }
    }

    private static async Task HandleTemporarySuppressions(
        ILogger logger,
        DirectoryInfo projectPath,
        FileInfo? buildFile,
        DirectoryInfo? temporarySuppressionsPath,
        bool temporarySuppressionAsExcel)
    {
        logger.LogInformation($"{EmojisConstants.AreaTemporarySuppression} Working on temporary suppressions");

        if (!FileHelper.ContainsSolutionOrProjectFile(projectPath) &&
            !FileHelper.IsSolutionOrProjectFile(buildFile))
        {
            logger.LogInformation("     Nothing to build! -projectPath do not contains a .sln or .csproj file");
            return;
        }

        var analyzerProviderBaseRules = await AnalyzerProviderBaseRulesHelper.GetAnalyzerProviderBaseRules(
            logger,
            ProviderCollectingMode.LocalCache,
            logWithAnsiConsoleMarkup: true);

        var stopwatch = Stopwatch.StartNew();
        logger.LogTrace("     Collecting build errors");
        var rootEditorConfigContent = string.Empty;
        if (temporarySuppressionsPath is null)
        {
            await EditorConfigHelper.UpdateRootFileRemoveCustomAtcAutogeneratedRuleSuppressions(projectPath);
        }
        else
        {
            rootEditorConfigContent = await EditorConfigHelper.ReadAllText(projectPath);
            DeleteSuppressionsFileInTempPath(temporarySuppressionsPath, temporarySuppressionAsExcel);
        }

        Dictionary<string, int> buildResult;

        try
        {
            buildResult = await DotnetBuildHelper.BuildAndCollectErrors(
                logger,
                projectPath,
                1,
                buildFile,
                useNugetRestore: true,
                useConfigurationReleaseMode: true,
                BuildDefaultTimeoutInSec,
                "     ");
        }
        catch (DataException ex)
        {
            logger.LogError($"{EmojisConstants.Error} {ex.Message}");
            return;
        }

        var suppressionLinesPrAnalyzer = GetSuppressionLines(analyzerProviderBaseRules, buildResult);
        if (!suppressionLinesPrAnalyzer.Any())
        {
            logger.LogTrace("     No suppressions to add.");
            return;
        }

        await EditorConfigHelper.UpdateRootFileAddCustomAtcAutogeneratedRuleSuppressions(projectPath, suppressionLinesPrAnalyzer);
        for (var i = 0; i < MaxNumberOfTimesToBuild; i++)
        {
            var runAgain = await BuildAndCollectErrorsAgainAndUpdateFile(
                logger,
                projectPath,
                2 + i,
                buildFile,
                buildResult,
                analyzerProviderBaseRules);

            if (!runAgain)
            {
                break;
            }
        }

        suppressionLinesPrAnalyzer = GetSuppressionLines(analyzerProviderBaseRules, buildResult);
        if (temporarySuppressionsPath is not null)
        {
            await EditorConfigHelper.WriteAllText(projectPath, rootEditorConfigContent);
            await CreateSuppressionsFileInTempPath(logger, temporarySuppressionsPath, temporarySuppressionAsExcel, suppressionLinesPrAnalyzer);
        }
        else
        {
            var totalSuppressions = suppressionLinesPrAnalyzer.Sum(x => x.Item2.Count);
            logger.LogInformation($"{EmojisConstants.FileUpdated}   [yellow]/[/]{EditorConfigHelper.FileNameEditorConfig} is updated with {totalSuppressions} suppressions");
        }

        stopwatch.Stop();
        logger.LogTrace($"     Collecting build errors time: {stopwatch.Elapsed.GetPrettyTime()}");
    }

    private static async Task<bool> BuildAndCollectErrorsAgainAndUpdateFile(
        ILogger logger,
        DirectoryInfo projectPath,
        int runNumber,
        FileInfo? buildFile,
        Dictionary<string, int> buildResult,
        Collection<AnalyzerProviderBaseRuleData> analyzerProviderBaseRules)
    {
        bool hasFoundNewErrors;

        try
        {
            var buildResultNextRun = await DotnetBuildHelper.BuildAndCollectErrors(
                logger,
                projectPath,
                runNumber,
                buildFile,
                useNugetRestore: true,
                useConfigurationReleaseMode: true,
                BuildDefaultTimeoutInSec,
                "     ");

            hasFoundNewErrors = buildResultNextRun.Count > 0;
            foreach (var item in buildResultNextRun)
            {
                if (buildResult.ContainsKey(item.Key))
                {
                    buildResult[item.Key] = buildResult[item.Key] + item.Value;
                }
                else
                {
                    buildResult.Add(item.Key, item.Value);
                }
            }
        }
        catch (DataException ex)
        {
            logger.LogError($"{EmojisConstants.Error} {ex.Message}");
            return false;
        }

        if (hasFoundNewErrors)
        {
            var suppressionLinesPrAnalyzer = GetSuppressionLines(analyzerProviderBaseRules, buildResult);
            if (suppressionLinesPrAnalyzer.Any())
            {
                await EditorConfigHelper.UpdateRootFileRemoveCustomAtcAutogeneratedRuleSuppressions(projectPath);
                await EditorConfigHelper.UpdateRootFileAddCustomAtcAutogeneratedRuleSuppressions(projectPath, suppressionLinesPrAnalyzer);
                return true;
            }
        }

        return false;
    }

    private static void DeleteSuppressionsFileInTempPath(
        DirectoryInfo temporarySuppressionsPath,
        bool temporarySuppressionAsExcel)
    {
        var temporarySuppressionsFile = Path.Join(
            temporarySuppressionsPath.FullName,
            temporarySuppressionAsExcel ? AtcCodingRulesSuppressionsFileNameAsExcel : AtcCodingRulesSuppressionsFileName);

        if (File.Exists(temporarySuppressionsFile))
        {
            File.Delete(temporarySuppressionsFile);
        }
    }

    private static Task CreateSuppressionsFileInTempPath(
        ILogger logger,
        DirectoryInfo temporarySuppressionsPath,
        bool temporarySuppressionAsExcel,
        IEnumerable<Tuple<string, List<string>>> suppressionLinesPrAnalyzer)
    {
        var temporarySuppressionsFile = Path.Join(
            temporarySuppressionsPath.FullName,
            temporarySuppressionAsExcel ? AtcCodingRulesSuppressionsFileNameAsExcel : AtcCodingRulesSuppressionsFileName);

        if (temporarySuppressionAsExcel)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var excelPackage = new ExcelPackage();

            excelPackage.Workbook.Properties.Author = "ATC-CodingRules-Updater";
            excelPackage.Workbook.Properties.Title = "Suppressions";
            excelPackage.Workbook.Properties.Subject = "Suppressions";
            excelPackage.Workbook.Properties.Created = DateTime.Now;

            var worksheet = excelPackage.Workbook.Worksheets.Add("Sheet 1");

            var rowNr = 1;
            worksheet.Cells[rowNr, 1].Value = "Code";
            worksheet.Cells[rowNr, 2].Value = "Occurrences";
            worksheet.Cells[rowNr, 3].Value = "Message";
            worksheet.Cells[rowNr, 4].Value = "HelpLink";
            rowNr++;

            foreach (var item in suppressionLinesPrAnalyzer)
            {
                foreach (var line in item.Item2)
                {
                    var sa = line.Split("#");
                    if (sa.Length != 2)
                    {
                        continue;
                    }

                    var code = sa[0]
                        .Replace("dotnet_diagnostic.", string.Empty, StringComparison.Ordinal)
                        .Replace(".severity = none", string.Empty, StringComparison.Ordinal)
                        .Trim();

                    var occurrenceAsTxt = sa[1]
                        .Substring(0, sa[1].IndexOf("occurrence", StringComparison.Ordinal))
                        .Trim();

                    var occurrence = int.Parse(occurrenceAsTxt, NumberStyles.Any, GlobalizationConstants.EnglishCultureInfo);

                    var afterOccurrence = sa[1]
                        .Substring(sa[1].IndexOf("occurrence", StringComparison.Ordinal))
                        .Replace("occurrences", string.Empty, StringComparison.Ordinal)
                        .Replace("occurrence", string.Empty, StringComparison.Ordinal)
                        .Trim();

                    string message;
                    var helpLink = string.Empty;
                    if (afterOccurrence.Length > 0)
                    {
                        var indexOfHttp = afterOccurrence.LastIndexOf("- http", StringComparison.Ordinal);
                        if (indexOfHttp != -1)
                        {
                            message = afterOccurrence.Substring(2, indexOfHttp - 2).Trim();
                            helpLink = afterOccurrence.Substring(indexOfHttp + 2).Trim();
                        }
                        else
                        {
                            message = afterOccurrence;
                        }
                    }
                    else
                    {
                        message = "Unknown";
                    }

                    worksheet.Cells[rowNr, 1].Value = code;
                    worksheet.Cells[rowNr, 2].Value = occurrence;
                    worksheet.Cells[rowNr, 3].Value = message;
                    worksheet.Cells[rowNr, 4].Value = helpLink;
                    rowNr++;
                }
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            worksheet.Cells["A1:D1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells["A1:D1"].Style.Fill.BackgroundColor.SetColor(Color.CornflowerBlue);
            worksheet.Cells["B2:B" + rowNr].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.View.FreezePanes(2, 1);

            logger.LogDebug($"{EmojisConstants.FileUpdated}   {temporarySuppressionsFile}");

            return excelPackage.SaveAsAsync(new FileInfo(temporarySuppressionsFile));
        }

        var sb = new StringBuilder();
        sb.AppendLine($"{EditorConfigHelper.AutogeneratedCustomSectionHeaderPrefix} {DateTime.Now:F}");
        foreach (var (analyzerName, suppressionLines) in suppressionLinesPrAnalyzer)
        {
            sb.AppendLine($"{Environment.NewLine}# {analyzerName}");

            foreach (var suppressionLine in suppressionLines)
            {
                sb.AppendLine(suppressionLine);
            }
        }

        logger.LogDebug($"{EmojisConstants.FileUpdated}   {temporarySuppressionsFile}");

        return File.WriteAllTextAsync(temporarySuppressionsFile, sb.ToString(), Encoding.UTF8);
    }

    private static List<Tuple<string, List<string>>> GetSuppressionLines(
        IReadOnlyCollection<AnalyzerProviderBaseRuleData> analyzerProviderBaseRules,
        Dictionary<string, int> buildResult)
    {
        var suppressionLines = new List<Tuple<string, string>>();
        var handledCodes = new List<string>();

        HandleSuppressionLinesForKnownAnalyzerRules(analyzerProviderBaseRules, buildResult, suppressionLines, handledCodes);
        HandleSuppressionLinesForUnknownAnalyzerRules(buildResult, suppressionLines, handledCodes);

        var groupedSuppressionLines = suppressionLines
            .GroupBy(x => x.Item1, StringComparer.Ordinal)
            .Select(group => new { AnalyzerName = group.Key, Values = group.Select(x => x.Item2).ToList() })
            .OrderBy(x => x.AnalyzerName)
            .ToList();

        return groupedSuppressionLines.Select(x => Tuple.Create(x.AnalyzerName, x.Values)).ToList();
    }

    private static void HandleSuppressionLinesForKnownAnalyzerRules(
        IReadOnlyCollection<AnalyzerProviderBaseRuleData> analyzerProviderBaseRules,
        Dictionary<string, int> buildResult,
        ICollection<Tuple<string, string>> suppressionLines,
        ICollection<string> handledCodes)
    {
        foreach (var (code, count) in buildResult.OrderBy(x => x.Key))
        {
            foreach (var analyzerProvider in analyzerProviderBaseRules)
            {
                var rule = analyzerProvider.Rules.FirstOrDefault(x => x.Code.Equals(code, StringComparison.Ordinal));
                if (rule is not null)
                {
                    var tabs = CalculateTabIndentationForSuppressionLine(rule.Code.Length);
                    var suppressionLine = string.IsNullOrEmpty(rule.Category)
                        ? $"dotnet_diagnostic.{code}.severity = none{tabs}# {count.Pluralize("occurrence")}{rule.TitleAndLink}"
                        : $"dotnet_diagnostic.{code}.severity = none{tabs}# {count.Pluralize("occurrence")} - Category: '{rule.Category}'{rule.TitleAndLink}";
                    suppressionLines.Add(Tuple.Create(analyzerProvider.Name, suppressionLine));
                    handledCodes.Add(code);
                }
            }
        }
    }

    private static void HandleSuppressionLinesForUnknownAnalyzerRules(
        Dictionary<string, int> buildResult,
        ICollection<Tuple<string, string>> suppressionLines,
        ICollection<string> handledCodes)
    {
        foreach (var (code, count) in buildResult.OrderBy(x => x.Key))
        {
            if (handledCodes.Contains(code))
            {
                continue;
            }

            var tabs = CalculateTabIndentationForSuppressionLine(code.Length);
            var suppressionLine = $"dotnet_diagnostic.{code}.severity = none{tabs}# {count.Pluralize("occurrence")}";
            suppressionLines.Add(Tuple.Create("Unknown", suppressionLine));
        }
    }

    private static string CalculateTabIndentationForSuppressionLine(
        int codeLength)
    {
        var tabs = codeLength switch
        {
            2 => "\t\t\t\t\t",
            3 => "\t\t\t\t",
            4 => "\t\t\t\t",
            5 => "\t\t\t\t",
            6 => "\t\t\t",
            7 => "\t\t\t",
            8 => "\t\t\t",
            9 => "\t\t\t",
            10 => "\t\t\t",
            11 => "\t\t",
            12 => "\t\t",
            13 => "\t\t",
            14 => "\t\t",
            15 => "\t",
            _ => "\t\t\t"
        };

        return tabs;
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
    private static string Pluralize(
        this int number,
        string word,
        string pluralSuffix = "s",
        string singularSuffix = "")
        => $@"{number} {word}{(number != 1 ? pluralSuffix : singularSuffix)}";
}