// ReSharper disable InvertIf
// ReSharper disable SuggestBaseTypeForParameter
namespace Atc.CodingRules.Updater.CLI.Helpers;

/// <summary>
/// The temporary-suppression step of <c>run</c>: builds the project, collects the analyzer
/// diagnostics it reports and writes them as temporary suppressions.
/// </summary>
public static class TemporarySuppressionHelper
{
    private const string AtcCodingRulesSuppressionsFileName = "AtcCodingRulesSuppressions.txt";
    private const string AtcCodingRulesSuppressionsFileNameAsExcel = "AtcCodingRulesSuppressions.xlsx";
    private const int MaxNumberOfTimesToBuild = 9;
    private const int BuildDefaultTimeoutInSec = 1200;

    /// <summary>
    /// The ATC rules set <c>TreatWarningsAsErrors</c>, which turns analyzer violations into build
    /// errors. A failing project stops MSBuild from building anything that depends on it, so those
    /// projects were never analysed and their violations were neither suppressed nor counted.
    /// Disabling the promotion lets the whole solution compile and be analysed in one pass.
    /// </summary>
    private const string CollectDiagnosticsBuildArguments = "-p:TreatWarningsAsErrors=false";

    /// <summary>
    /// Runs the build/suppress loop configured by <paramref name="options"/>.
    /// </summary>
    public static Task HandleAsync(
        ILogger logger,
        DirectoryInfo projectPath,
        OptionsFile options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);

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

        var buildOptions = new SuppressionBuildOptions(
            IsReleaseConfiguration(options.BuildConfiguration),
            BuildAdditionalArguments(options.BuildProperties));

        return HandleTemporarySuppressions(
            logger,
            projectPath,
            buildFile,
            temporarySuppressionsPath,
            options.TemporarySuppressionAsExcel,
            buildOptions,
            cancellationToken);
    }

    /// <summary>
    /// Formats <paramref name="buildProperties"/> as MSBuild <c>-p:Name=Value</c> switches.
    /// </summary>
    /// <remarks>
    /// MSBuild offers no way to skip a named target, so forwarding properties is the mechanism a
    /// user has for turning off a conditioned target (an obfuscator, a signing step) during the
    /// suppression build. Entries without a <c>Name=Value</c> shape are ignored rather than passed
    /// through as a malformed switch.
    /// </remarks>
    internal static string BuildAdditionalArguments(
        IEnumerable<string> buildProperties)
    {
        ArgumentNullException.ThrowIfNull(buildProperties);

        var switches = new List<string>();

        foreach (var property in buildProperties)
        {
            if (string.IsNullOrWhiteSpace(property))
            {
                continue;
            }

            var separatorIndex = property.IndexOf('=', StringComparison.Ordinal);
            if (separatorIndex <= 0 || separatorIndex == property.Length - 1)
            {
                continue;
            }

            switches.Add(property.Contains(' ', StringComparison.Ordinal)
                ? $"-p:\"{property}\""
                : $"-p:{property}");
        }

        return string.Join(' ', switches);
    }

    /// <summary>
    /// Maps a configuration name onto the release/debug flag the build helper takes. Release stays
    /// the default, so omitting <c>--buildConfiguration</c> preserves the previous behaviour.
    /// </summary>
    internal static bool IsReleaseConfiguration(string? configuration)
        => string.IsNullOrWhiteSpace(configuration) ||
           configuration.Equals("Release", StringComparison.OrdinalIgnoreCase);

    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "OK.")]
    private static async Task HandleTemporarySuppressions(
        ILogger logger,
        DirectoryInfo projectPath,
        FileInfo? buildFile,
        DirectoryInfo? temporarySuppressionsPath,
        bool temporarySuppressionAsExcel,
        SuppressionBuildOptions buildOptions,
        CancellationToken cancellationToken)
    {
        logger.LogInformation($"{AppEmojisConstants.AreaTemporarySuppression} Working on temporary suppressions");

        if (!buildOptions.UseReleaseConfiguration || buildOptions.AdditionalBuildArguments.Length > 0)
        {
            logger.LogTrace(
                $"     Build configuration: {(buildOptions.UseReleaseConfiguration ? "Release" : "Debug")} {buildOptions.AdditionalBuildArguments}".TrimEnd());
        }

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
            rootEditorConfigContent = await EditorConfigHelper.ReadAllText(projectPath, cancellationToken);
            DeleteSuppressionsFileInTempPath(temporarySuppressionsPath, temporarySuppressionAsExcel);
        }

        Dictionary<string, int> buildResult;

        try
        {
            // First pass collects errors, purely so a genuinely broken build (MSB/NU) can be
            // reported before any suppression is written. Counting happens in the second pass.
            buildResult = await DotnetBuildHelper.BuildAndCollectErrors(
                logger,
                projectPath,
                1,
                buildFile,
                useNugetRestore: true,
                useConfigurationReleaseMode: buildOptions.UseReleaseConfiguration,
                BuildDefaultTimeoutInSec,
                "     ",
                buildOptions.AdditionalBuildArguments,
                cancellationToken);
        }
        catch (DataException ex)
        {
            logger.LogError($"{EmojisConstants.Error} {Markup.Escape(ex.Message)}");
            return;
        }
        catch (IOException ex)
        {
            logger.LogError($"{EmojisConstants.Error} {Markup.Escape(ex.Message)}");
            return;
        }

        if (buildResult.Any(x => x.Key.StartsWith("MSB", StringComparison.Ordinal)))
        {
            var errorTypes = buildResult
                .Where(x => x.Key.StartsWith("MSB", StringComparison.Ordinal))
                .Select(x => x.Key)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList();

            logger.LogWarning($"     MSB-errors ({string.Join(',', errorTypes)}) was found, please correct them manually first and try again.");
        }
        else if (buildResult.Any(x => x.Key.StartsWith("NU", StringComparison.Ordinal)))
        {
            var errorTypes = buildResult
                .Where(x => x.Key.StartsWith("NU", StringComparison.Ordinal))
                .Select(x => x.Key)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList();

            logger.LogWarning($"     NU-errors ({string.Join(',', errorTypes)}) was found, please correct them manually first and try again.");
        }
        else
        {
            // Second pass does the counting. With warnings-as-errors disabled the whole solution
            // compiles, so projects that depend on a violating project are analysed too - without
            // this their violations were invisible and the generated list was incomplete, not
            // merely miscounted.
            try
            {
                buildResult = await CollectDiagnosticsForSuppressions(
                    logger,
                    projectPath,
                    2,
                    buildFile,
                    buildOptions,
                    cancellationToken);
            }
            catch (DataException ex)
            {
                logger.LogError($"{EmojisConstants.Error} {Markup.Escape(ex.Message)}");
                return;
            }

            var suppressionLinesPrAnalyzer = GetSuppressionLines(analyzerProviderBaseRules, buildResult);
            if (suppressionLinesPrAnalyzer.Count > 0)
            {
                await EditorConfigHelper.UpdateRootFileAddCustomAtcAutogeneratedRuleSuppressions(projectPath, suppressionLinesPrAnalyzer);
                var converged = false;
                for (var i = 0; i < MaxNumberOfTimesToBuild; i++)
                {
                    var runAgain = await BuildAndCollectErrorsAgainAndUpdateFile(
                        logger,
                        projectPath,
                        3 + i,
                        buildFile,
                        buildResult,
                        analyzerProviderBaseRules,
                        buildOptions,
                        cancellationToken);

                    if (!runAgain)
                    {
                        converged = true;
                        break;
                    }
                }

                if (!converged)
                {
                    logger.LogWarning($"{Emoji.Known.Warning}   Build loop did not converge after {MaxNumberOfTimesToBuild + 1} build runs; review the project for analyzers that emit different diagnostics on each pass.");
                }

                suppressionLinesPrAnalyzer = GetSuppressionLines(analyzerProviderBaseRules, buildResult);
                if (temporarySuppressionsPath is not null)
                {
                    await EditorConfigHelper.WriteAllText(projectPath, rootEditorConfigContent, cancellationToken);
                    await CreateSuppressionsFileInTempPath(logger, temporarySuppressionsPath, temporarySuppressionAsExcel, suppressionLinesPrAnalyzer);
                }
                else
                {
                    var totalSuppressions = suppressionLinesPrAnalyzer.Sum(x => x.Item2.Count);
                    logger.LogInformation($"{EmojisConstants.FileUpdated}   [yellow]/[/]{EditorConfigHelper.FileName} is updated with {totalSuppressions} suppressions");
                }
            }
            else
            {
                logger.LogTrace("     No suppressions to add.");
            }
        }

        stopwatch.Stop();
        logger.LogTrace($"     Collecting build errors time: {stopwatch.Elapsed.GetPrettyTime()}");
    }

    /// <summary>
    /// Builds and collects the analyzer diagnostics used to generate suppressions.
    /// </summary>
    /// <remarks>
    /// Collects <em>warnings</em> with <c>TreatWarningsAsErrors</c> disabled, so the whole solution
    /// compiles and every project is analysed. Any <c>--buildProperty</c> values the caller passed
    /// are appended after the override, so a user can still override it themselves.
    /// </remarks>
    internal static Task<Dictionary<string, int>> CollectDiagnosticsForSuppressions(
        ILogger logger,
        DirectoryInfo projectPath,
        int runNumber,
        FileInfo? buildFile,
        SuppressionBuildOptions buildOptions,
        CancellationToken cancellationToken)
        => DotnetBuildHelper.BuildAndCollectWarnings(
            logger,
            projectPath,
            runNumber,
            buildFile,
            useNugetRestore: true,
            useConfigurationReleaseMode: buildOptions.UseReleaseConfiguration,
            BuildDefaultTimeoutInSec,
            "     ",
            CombineBuildArguments(buildOptions),
            cancellationToken);

    /// <summary>
    /// Puts the warnings-as-errors override first so a user-supplied <c>--buildProperty</c> with
    /// the same name still wins.
    /// </summary>
    internal static string CombineBuildArguments(
        SuppressionBuildOptions buildOptions)
    {
        ArgumentNullException.ThrowIfNull(buildOptions);

        return string.IsNullOrEmpty(buildOptions.AdditionalBuildArguments)
            ? CollectDiagnosticsBuildArguments
            : $"{CollectDiagnosticsBuildArguments} {buildOptions.AdditionalBuildArguments}";
    }

    private static async Task<bool> BuildAndCollectErrorsAgainAndUpdateFile(
        ILogger logger,
        DirectoryInfo projectPath,
        int runNumber,
        FileInfo? buildFile,
        Dictionary<string, int> buildResult,
        Collection<AnalyzerProviderBaseRuleData> analyzerProviderBaseRules,
        SuppressionBuildOptions buildOptions,
        CancellationToken cancellationToken)
    {
        bool hasFoundNewErrors;

        try
        {
            var buildResultNextRun = await CollectDiagnosticsForSuppressions(
                logger,
                projectPath,
                runNumber,
                buildFile,
                buildOptions,
                cancellationToken);

            hasFoundNewErrors = buildResultNextRun.Count > 0;
            foreach (var (key, value) in buildResultNextRun)
            {
                if (!buildResult.TryAdd(key, value))
                {
                    buildResult[key] += value;
                }
            }
        }
        catch (DataException ex)
        {
            logger.LogError($"{EmojisConstants.Error} {Markup.Escape(ex.Message)}");
            return false;
        }

        if (hasFoundNewErrors)
        {
            var suppressionLinesPrAnalyzer = GetSuppressionLines(analyzerProviderBaseRules, buildResult);
            if (suppressionLinesPrAnalyzer.Count > 0)
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

    private static async Task CreateSuppressionsFileInTempPath(
        ILogger logger,
        DirectoryInfo temporarySuppressionsPath,
        bool temporarySuppressionAsExcel,
        IList<Tuple<string, List<string>>> suppressionLinesPrAnalyzer)
    {
        var temporarySuppressionsFile = Path.Join(
            temporarySuppressionsPath.FullName,
            temporarySuppressionAsExcel ? AtcCodingRulesSuppressionsFileNameAsExcel : AtcCodingRulesSuppressionsFileName);

        if (temporarySuppressionAsExcel)
        {
            ExcelPackage.License.SetNonCommercialOrganization("ATC-Net");

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

            rowNr = AddSuppressionLinesToWorksheet(worksheet, suppressionLinesPrAnalyzer, rowNr);

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            worksheet.Cells["A1:D1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells["A1:D1"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.CornflowerBlue);
            worksheet.Cells["B2:B" + rowNr].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.View.FreezePanes(2, 1);

            logger.LogDebug($"{EmojisConstants.FileUpdated}   {temporarySuppressionsFile} updated");
            await excelPackage.SaveAsAsync(new FileInfo(temporarySuppressionsFile));
        }

        var suppressionsText = CreateSuppressionsText(suppressionLinesPrAnalyzer);

        logger.LogDebug($"{EmojisConstants.FileUpdated}   {temporarySuppressionsFile} updated");

        await Atc.Helpers.FileHelper.WriteAllTextAsync(new FileInfo(temporarySuppressionsFile), suppressionsText);
    }

    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "OK.")]
    private static int AddSuppressionLinesToWorksheet(
        ExcelWorksheet worksheet,
        IEnumerable<Tuple<string, List<string>>> suppressionLinesPrAnalyzer,
        int rowNr)
    {
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
                        message = afterOccurrence
                            .Substring(2, indexOfHttp - 2)
                            .Trim();

                        helpLink = afterOccurrence
                            .Substring(indexOfHttp + 2)
                            .Trim();
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

        return rowNr;
    }

    internal static string CreateSuppressionsText(
        IEnumerable<Tuple<string, List<string>>> suppressionLinesPrAnalyzer)
    {
        var sb = new StringBuilder();
        sb.AppendLine(GlobalizationConstants.EnglishCultureInfo, $"{EditorConfigHelper.AutogeneratedCustomSectionHeaderPrefix} {DateTime.Now:F}");
        sb.AppendLine(GlobalizationConstants.EnglishCultureInfo, $"# {CodingRulesUpdaterVersionHelper.GetGeneratedByText()}");
        foreach (var (analyzerName, suppressionLines) in suppressionLinesPrAnalyzer)
        {
            sb.AppendLine(GlobalizationConstants.EnglishCultureInfo, $"{Environment.NewLine}# {analyzerName}");

            foreach (var suppressionLine in suppressionLines)
            {
                sb.AppendLine(suppressionLine);
            }
        }

        return sb.ToString();
    }

    internal static List<Tuple<string, List<string>>> GetSuppressionLines(
        IReadOnlyCollection<AnalyzerProviderBaseRuleData> analyzerProviderBaseRules,
        Dictionary<string, int> buildResult)
    {
        var suppressionLines = new List<Tuple<string, string>>();
        var handledCodes = new HashSet<string>(StringComparer.Ordinal);

        HandleSuppressionLinesForKnownAnalyzerRules(analyzerProviderBaseRules, buildResult, suppressionLines, handledCodes);
        HandleSuppressionLinesForUnknownAnalyzerRules(buildResult, suppressionLines, handledCodes);

        var groupedSuppressionLines = suppressionLines
            .GroupBy(x => x.Item1, StringComparer.Ordinal)
            .Select(group => new
            {
                AnalyzerName = group.Key,
                Values = group
                    .Select(x => x.Item2)
                    .ToList(),
            })
            .OrderBy(x => x.AnalyzerName, StringComparer.Ordinal)
            .ToList();

        return groupedSuppressionLines
            .Select(x => Tuple.Create(x.AnalyzerName, x.Values))
            .ToList();
    }

    private static void HandleSuppressionLinesForKnownAnalyzerRules(
        IReadOnlyCollection<AnalyzerProviderBaseRuleData> analyzerProviderBaseRules,
        Dictionary<string, int> buildResult,
        ICollection<Tuple<string, string>> suppressionLines,
        ISet<string> handledCodes)
    {
        // Index the rule catalog once instead of re-scanning every provider's full rule list for
        // each build error. TryAdd means the first provider to document a code owns it, so a code
        // appearing in two catalogs still produces exactly one dotnet_diagnostic entry.
        var rulesByCode = new Dictionary<string, (string ProviderName, AnalyzerProviders.Models.Rule Rule)>(StringComparer.Ordinal);
        foreach (var analyzerProvider in analyzerProviderBaseRules)
        {
            foreach (var rule in analyzerProvider.Rules)
            {
                rulesByCode.TryAdd(rule.Code, (analyzerProvider.Name, rule));
            }
        }

        foreach (var (code, count) in buildResult.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            if (!rulesByCode.TryGetValue(code, out var match))
            {
                continue;
            }

            var rule = match.Rule;
            var tabs = CalculateTabIndentationForSuppressionLine(rule.Code.Length);
            var suppressionLine = string.IsNullOrEmpty(rule.Category)
                ? $"dotnet_diagnostic.{code}.severity = suggestion{tabs}# {count.Pluralize("occurrence")}{rule.TitleAndLink}"
                : $"dotnet_diagnostic.{code}.severity = suggestion{tabs}# {count.Pluralize("occurrence")} - Category: '{rule.Category}'{rule.TitleAndLink}";
            suppressionLines.Add(Tuple.Create(match.ProviderName, suppressionLine));
            handledCodes.Add(code);
        }
    }

    private static void HandleSuppressionLinesForUnknownAnalyzerRules(
        Dictionary<string, int> buildResult,
        ICollection<Tuple<string, string>> suppressionLines,
        ISet<string> handledCodes)
    {
        foreach (var (code, count) in buildResult.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            if (handledCodes.Contains(code))
            {
                continue;
            }

            var tabs = CalculateTabIndentationForSuppressionLine(code.Length);
            var suppressionLine = $"dotnet_diagnostic.{code}.severity = suggestion{tabs}# {count.Pluralize("occurrence")}";
            suppressionLines.Add(Tuple.Create("Unknown", suppressionLine));
        }
    }

    private static string CalculateTabIndentationForSuppressionLine(
        int codeLength)
        => codeLength switch
        {
            >= 1 and <= 3 => "\t\t\t",
            >= 4 and <= 7 => "\t\t",
            _ => "\t",
        };

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
        => $"{number} {word}{(number != 1 ? pluralSuffix : singularSuffix)}";
}