using System.Drawing;

using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace Atc.CodingRules.Updater.CLI;

public static class ConfigHelper
{
    // TODO: Use this "multi-distributions" branch for now.
    private const string RawCodingRulesDistributionBaseUrl = "https://raw.githubusercontent.com/atc-net/atc-coding-rules/feature/multi-distributions/distribution";
    ////private const string RawCodingRulesDistributionBaseUrl = "https://raw.githubusercontent.com/atc-net/atc-coding-rules/main/distribution";
    private const string AtcCodingRulesSuppressionsFileName = "AtcCodingRulesSuppressions.txt";

    private const string AtcCodingRulesSuppressionsFileNameAsExcel = "AtcCodingRulesSuppressions.xlsx";
    private const int MaxNumberOfTimesToBuild = 9;

    public static async Task HandleFiles(
        ILogger logger,
        DirectoryInfo rootPath,
        OptionRoot options,
        FileInfo? buildFile)
    {
        ArgumentNullException.ThrowIfNull(rootPath);
        ArgumentNullException.ThrowIfNull(options);

        var isFirstTime = IsFirstTime(rootPath);

        HandleEditorConfigFiles(logger, rootPath, options, isFirstTime);
        HandleDirectoryBuildPropsFiles(logger, rootPath, options, isFirstTime);

        if (!isFirstTime &&
            options.UseTemporarySuppressions)
        {
            var temporarySuppressionsPath = string.IsNullOrEmpty(options.TemporarySuppressionsPath)
                ? rootPath
                : new DirectoryInfo(options.TemporarySuppressionsPath);

            await HandleTemporarySuppressions(
                logger,
                rootPath,
                buildFile,
                temporarySuppressionsPath,
                options.TemporarySuppressionAsExcel);
        }
    }

    private static void HandleEditorConfigFiles(
        ILogger logger,
        DirectoryInfo rootPath,
        OptionRoot options,
        bool isFirstTime)
    {
        logger.LogInformation($"{EmojisConstants.AreaEditorConfig} Working on EditorConfig files");

        var rawCodingRulesDistributionSolutionTargetBaseUrl = $"{RawCodingRulesDistributionBaseUrl}/{options.SolutionTarget}";
        EditorConfigHelper.HandleFile(logger, isFirstTime, "root", rawCodingRulesDistributionSolutionTargetBaseUrl, rootPath, string.Empty);

        foreach (var item in options.Mappings.Sample.Paths)
        {
            var path = new DirectoryInfo(item);
            EditorConfigHelper.HandleFile(logger, isFirstTime, "sample", rawCodingRulesDistributionSolutionTargetBaseUrl, path, "sample");
        }

        foreach (var item in options.Mappings.Src.Paths)
        {
            var path = new DirectoryInfo(item);
            EditorConfigHelper.HandleFile(logger, isFirstTime, "src", rawCodingRulesDistributionSolutionTargetBaseUrl, path, "src");
        }

        foreach (var item in options.Mappings.Test.Paths)
        {
            var path = new DirectoryInfo(item);
            EditorConfigHelper.HandleFile(logger, isFirstTime, "test", rawCodingRulesDistributionSolutionTargetBaseUrl, path, "test");
        }
    }

    private static void HandleDirectoryBuildPropsFiles(
        ILogger logger,
        DirectoryInfo rootPath,
        OptionRoot options,
        bool isFirstTime)
    {
        logger.LogInformation($"{EmojisConstants.AreaDirectoryBuildProps} Working on Directory.Build.props files");
        var rawCodingRulesDistributionSolutionTargetBaseUrl = $"{RawCodingRulesDistributionBaseUrl}/{options.SolutionTarget}";

        DirectoryBuildPropsHelper.HandleFile(logger, isFirstTime, "root", rawCodingRulesDistributionSolutionTargetBaseUrl, options.UseLatestMinorNugetVersion, rootPath, string.Empty);

        foreach (var item in options.Mappings.Sample.Paths)
        {
            var path = new DirectoryInfo(item);
            DirectoryBuildPropsHelper.HandleFile(logger, isFirstTime, "sample", rawCodingRulesDistributionSolutionTargetBaseUrl, options.UseLatestMinorNugetVersion, path, "sample");
        }

        foreach (var item in options.Mappings.Src.Paths)
        {
            var path = new DirectoryInfo(item);
            DirectoryBuildPropsHelper.HandleFile(logger, isFirstTime, "src", rawCodingRulesDistributionSolutionTargetBaseUrl, options.UseLatestMinorNugetVersion, path, "src");
        }

        foreach (var item in options.Mappings.Test.Paths)
        {
            var path = new DirectoryInfo(item);
            DirectoryBuildPropsHelper.HandleFile(logger, isFirstTime, "test", rawCodingRulesDistributionSolutionTargetBaseUrl, options.UseLatestMinorNugetVersion, path, "test");
        }
    }

    private static bool IsFirstTime(DirectoryInfo rootPath)
    {
        var file = new FileInfo(Path.Combine(rootPath.FullName, EditorConfigHelper.FileNameEditorConfig));
        return !file.Exists;
    }

    private static async Task HandleTemporarySuppressions(
        ILogger logger,
        DirectoryInfo rootPath,
        FileInfo? buildFile,
        DirectoryInfo? temporarySuppressionsPath,
        bool temporarySuppressionAsExcel)
    {
        logger.LogInformation($"{EmojisConstants.AreaTemporarySuppression} Working on temporary suppressions");

        var analyzerProviderBaseRules = await AnalyzerProviderBaseRulesHelper.GetAnalyzerProviderBaseRules(logger, ProviderCollectingMode.LocalCache);

        var rootEditorConfigContent = string.Empty;

        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation("Started collecting build errors");

        if (temporarySuppressionsPath is null)
        {
            await EditorConfigHelper.UpdateRootFileRemoveCustomAtcAutogeneratedRuleSuppressions(rootPath);
        }
        else
        {
            rootEditorConfigContent = await EditorConfigHelper.ReadAllText(rootPath);
            DeleteSuppressionsFileInTempPath(temporarySuppressionsPath, temporarySuppressionAsExcel);
        }

        Dictionary<string, int> buildResult;

        try
        {
            buildResult = DotnetBuildHelper.BuildAndCollectErrors(logger, rootPath, 1, buildFile);
        }
        catch (DataException ex)
        {
            logger.LogError($"{EmojisConstants.Error} {ex.Message}");
            return;
        }

        var suppressionLinesPrAnalyzer = GetSuppressionLines(analyzerProviderBaseRules, buildResult);
        if (!suppressionLinesPrAnalyzer.Any())
        {
            logger.LogInformation("No suppressions to add.");
            return;
        }

        await EditorConfigHelper.UpdateRootFileAddCustomAtcAutogeneratedRuleSuppressions(rootPath, suppressionLinesPrAnalyzer);
        for (var i = 0; i < MaxNumberOfTimesToBuild; i++)
        {
            var runAgain = await BuildAndCollectErrorsAgainAndUpdateFile(
                logger,
                rootPath,
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
            await EditorConfigHelper.WriteAllText(rootPath, rootEditorConfigContent);
            await CreateSuppressionsFileInTempPath(logger, temporarySuppressionsPath, temporarySuppressionAsExcel, suppressionLinesPrAnalyzer);
        }
        else
        {
            var totalSuppressions = suppressionLinesPrAnalyzer.Sum(x => x.Item2.Count);
            logger.LogDebug($"{EditorConfigHelper.FileNameEditorConfig} is updated with {totalSuppressions} suppressions");
        }

        stopwatch.Stop();
        logger.LogInformation($"Finished collecting build errors - Elapsed time: {stopwatch.Elapsed:mm\\:ss}");
    }

    private static async Task<bool> BuildAndCollectErrorsAgainAndUpdateFile(
        ILogger logger,
        DirectoryInfo rootPath,
        int runNumber,
        FileInfo? buildFile,
        Dictionary<string, int> buildResult,
        Collection<AnalyzerProviderBaseRuleData> analyzerProviderBaseRules)
    {
        bool hasFoundNewErrors;

        try
        {
            var buildResultNextRun = DotnetBuildHelper.BuildAndCollectErrors(logger, rootPath, runNumber, buildFile);
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
                await EditorConfigHelper.UpdateRootFileRemoveCustomAtcAutogeneratedRuleSuppressions(rootPath);
                await EditorConfigHelper.UpdateRootFileAddCustomAtcAutogeneratedRuleSuppressions(rootPath, suppressionLinesPrAnalyzer);
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
                        ? $"dotnet_diagnostic.{code}.severity = none{tabs}# {count.Pluralize("occurrence")} - {rule.Title} - {rule.Link}"
                        : $"dotnet_diagnostic.{code}.severity = none{tabs}# {count.Pluralize("occurrence")} - Category: '{rule.Category}' - {rule.Title} - {rule.Link}";
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