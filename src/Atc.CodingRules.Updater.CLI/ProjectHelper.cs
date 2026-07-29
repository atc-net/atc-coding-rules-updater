// ReSharper disable SwitchStatementMissingSomeEnumCasesNoDefault
// ReSharper disable InvertIf
// ReSharper disable SuggestBaseTypeForParameter
namespace Atc.CodingRules.Updater.CLI;

public static class ProjectHelper
{
    private const string AtcCodingRulesSuppressionsFileName = "AtcCodingRulesSuppressions.txt";
    private const string AtcCodingRulesSuppressionsFileNameAsExcel = "AtcCodingRulesSuppressions.xlsx";
    private const int MaxNumberOfTimesToBuild = 9;
    private const int BuildDefaultTimeoutInSec = 1200;

    private static readonly string RawCodingRulesDistributionBaseUrl = Constants.GitRawContentUrl + "/atc-net/atc-coding-rules/main/distribution";

    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "Sequential phases of one run; splitting hurts readability.")]
    public static async Task<RunSummary> HandleFiles(
        ILogger logger,
        DirectoryInfo projectPath,
        OptionsFile options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(projectPath);
        ArgumentNullException.ThrowIfNull(options);

        var summary = new RunSummary { DryRun = options.DryRun };

        if (options.DryRun)
        {
            logger.LogInformation("[yellow]Dry-run mode is active — no files will be written.[/]");
        }

        ProjectSanityCheckHelper.CheckFiles(
            throwIf: true,
            logger,
            projectPath,
            options.ProjectTarget);

        // The per-file handlers below are sequential and synchronous. Warming the cache first
        // turns N serial round-trips into one concurrent batch without restructuring them.
        PrefetchDistributionFiles(logger, projectPath, options, cancellationToken);

        HandleEditorConfigFiles(logger, projectPath, options, summary);

        if (options.ProjectTarget
            is SupportedProjectTargetType.DotNetCore
            or SupportedProjectTargetType.DotNet5
            or SupportedProjectTargetType.DotNet6
            or SupportedProjectTargetType.DotNet7
            or SupportedProjectTargetType.DotNet8
            or SupportedProjectTargetType.DotNet9
            or SupportedProjectTargetType.DotNet10
            or SupportedProjectTargetType.DotNet11)
        {
            HandleDirectoryBuildPropsFiles(logger, projectPath, options, summary);

            if (options.UseTemporarySuppressions && options.DryRun)
            {
                logger.LogInformation($"{AppEmojisConstants.AreaTemporarySuppression} [dim](dry-run)[/] would run the build/suppress loop — skipped");
            }
            else if (options.UseTemporarySuppressions)
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

                await HandleTemporarySuppressions(
                    logger,
                    projectPath,
                    buildFile,
                    temporarySuppressionsPath,
                    options.TemporarySuppressionAsExcel,
                    cancellationToken);
            }
        }

        return summary;
    }

    /// <summary>
    /// Resolves <paramref name="projectPath"/> to an existing directory, or logs a clear error and
    /// returns <c>null</c>. Without this a mistyped <c>--projectPath</c> surfaces as an unhandled
    /// <see cref="DirectoryNotFoundException"/> from deep inside the options loader.
    /// </summary>
    internal static DirectoryInfo? GetExistingProjectPath(
        ILogger logger,
        string projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            logger.LogError($"{EmojisConstants.Error} No project path was given - use --projectPath");
            return null;
        }

        var directory = new DirectoryInfo(projectPath);
        if (!directory.Exists)
        {
            logger.LogError($"{EmojisConstants.Error} Project path does not exist: {Markup.Escape(directory.FullName)}");
            return null;
        }

        return directory;
    }

    /// <summary>
    /// Runs the sanity checks in report-only mode and returns every diagnostic found, so the
    /// caller can map them to an exit code.
    /// </summary>
    public static IReadOnlyList<SanityCheckDiagnostic> SanityCheckFiles(
        ILogger logger,
        DirectoryInfo projectPath,
        OptionsFile options)
    {
        ArgumentNullException.ThrowIfNull(projectPath);
        ArgumentNullException.ThrowIfNull(options);

        return ProjectSanityCheckHelper.CheckFiles(
            throwIf: false,
            logger,
            projectPath,
            options.ProjectTarget);
    }

    /// <summary>
    /// Collects every distribution URL this run will ask for and downloads them concurrently into
    /// the process cache.
    /// </summary>
    private static void PrefetchDistributionFiles(
        ILogger logger,
        DirectoryInfo projectPath,
        OptionsFile options,
        CancellationToken cancellationToken)
    {
        var targetBaseUrl = $"{RawCodingRulesDistributionBaseUrl}/{options.ProjectTarget.ToStringLowerCase()}";
        var projectFrameworkBaseUrl = $"{RawCodingRulesDistributionBaseUrl}/project-frameworks";

        var urls = new List<string>
        {
            $"{targetBaseUrl}/{EditorConfigHelper.FileName}",
            $"{targetBaseUrl}/{DirectoryBuildPropsHelper.FileName}",
        };

        foreach (var (area, _) in EnumerateMappedPaths(options))
        {
            urls.Add($"{targetBaseUrl}/{area}/{EditorConfigHelper.FileName}");
            urls.Add($"{targetBaseUrl}/{area}/{DirectoryBuildPropsHelper.FileName}");
        }

        foreach (var (csProjFile, projectType) in DotnetCsProjFileHelper.FindAllInPathAndPredictProjectTypes(projectPath))
        {
            var projectFrameworkType = DetermineProjectFrameworkType(options, csProjFile, projectType);
            if (projectFrameworkType == ProjectFrameworkType.None)
            {
                continue;
            }

            urls.Add($"{projectFrameworkBaseUrl}/{projectFrameworkType.ToStringLowerCase()}/{EditorConfigHelper.FileName}");
        }

        HttpClientHelper.Prefetch(logger, urls, cancellationToken);
    }

    private static void HandleEditorConfigFiles(
        ILogger logger,
        DirectoryInfo projectPath,
        OptionsFile options,
        RunSummary summary)
    {
        logger.LogInformation($"{AppEmojisConstants.AreaEditorConfig} Working on EditorConfig files");

        var rawCodingRulesDistributionProjectTargetBaseUrl = $"{RawCodingRulesDistributionBaseUrl}/{options.ProjectTarget.ToStringLowerCase()}";
        var projectFrameworkCodingRulesBaseUrl = $"{RawCodingRulesDistributionBaseUrl}/project-frameworks";

        summary.Files.Add(new RunFileResult(
            "root",
            EditorConfigHelper.FileName,
            Path.Combine(projectPath.FullName, EditorConfigHelper.FileName),
            EditorConfigHelper.HandleFile(logger, "root", rawCodingRulesDistributionProjectTargetBaseUrl, projectPath, string.Empty, options.DryRun)));

        foreach (var (area, path) in EnumerateMappedPaths(options))
        {
            summary.Files.Add(new RunFileResult(
                area,
                EditorConfigHelper.FileName,
                Path.Combine(path.FullName, EditorConfigHelper.FileName),
                EditorConfigHelper.HandleFile(logger, area, rawCodingRulesDistributionProjectTargetBaseUrl, path, area, options.DryRun)));
        }

        // Handle Project specific Frameworks
        var projectsInProjectPath = DotnetCsProjFileHelper.FindAllInPathAndPredictProjectTypes(projectPath);

        foreach (var (csProjFile, projectType) in projectsInProjectPath)
        {
            var projectFrameworkType = DetermineProjectFrameworkType(options, csProjFile, projectType);
            if (projectFrameworkType == ProjectFrameworkType.None)
            {
                continue;
            }

            var csProjDirectory = csProjFile.Directory!;

            summary.Files.Add(new RunFileResult(
                projectFrameworkType.ToStringLowerCase(),
                EditorConfigHelper.FileName,
                Path.Combine(csProjDirectory.FullName, EditorConfigHelper.FileName),
                EditorConfigHelper.HandleFile(
                    logger,
                    "ProjectFramework",
                    projectFrameworkCodingRulesBaseUrl,
                    csProjDirectory,
                    projectFrameworkType.ToStringLowerCase(),
                    options.DryRun)));
        }
    }

    private static ProjectFrameworkType DetermineProjectFrameworkType(
        OptionsFile options,
        FileInfo csProjFile,
        DotnetProjectType projectType)
    {
        var projectFrameworkType = ProjectFrameworkType.None;

        var optionsProjectFrameworkMapping = options.ProjectFrameworkMappings.FirstOrDefault(
            x => x.Name.Equals(
                Path.GetFileNameWithoutExtension(csProjFile.Name),
                StringComparison.OrdinalIgnoreCase));

        projectFrameworkType = optionsProjectFrameworkMapping?.Type ?? projectType switch
        {
            DotnetProjectType.AspireAppHost or DotnetProjectType.AspireServiceDefaults => ProjectFrameworkType.Aspire,
            DotnetProjectType.AzureFunctionApp => ProjectFrameworkType.AzureFunctions,
            DotnetProjectType.BlazorServerApp or DotnetProjectType.BlazorWAsmApp => ProjectFrameworkType.Blazor,
            DotnetProjectType.CliApp => ProjectFrameworkType.Cli,
            DotnetProjectType.MauiApp => ProjectFrameworkType.Maui,
            DotnetProjectType.WinFormApp => ProjectFrameworkType.WinForms,
            DotnetProjectType.WpfApp or DotnetProjectType.WpfLibrary => ProjectFrameworkType.Wpf,
            DotnetProjectType.WebApi => ProjectFrameworkType.WebApi,
            _ => projectFrameworkType,
        };

        return projectFrameworkType;
    }

    private static void HandleDirectoryBuildPropsFiles(
        ILogger logger,
        DirectoryInfo projectPath,
        OptionsFile options,
        RunSummary summary)
    {
        logger.LogInformation($"{AppEmojisConstants.AreaDirectoryBuildProps} Working on Directory.Build.props files");
        var rawCodingRulesDistributionProjectTargetBaseUrl = $"{RawCodingRulesDistributionBaseUrl}/{options.ProjectTarget.ToStringLowerCase()}";

        Record(
            summary,
            "root",
            projectPath,
            DirectoryBuildPropsHelper.HandleFile(logger, "root", rawCodingRulesDistributionProjectTargetBaseUrl, options.UseLatestMinorNugetVersion, projectPath, string.Empty, options.DryRun, options.ForceNugetRefresh));

        foreach (var (area, path) in EnumerateMappedPaths(options))
        {
            Record(
                summary,
                area,
                path,
                DirectoryBuildPropsHelper.HandleFile(logger, area, rawCodingRulesDistributionProjectTargetBaseUrl, options.UseLatestMinorNugetVersion, path, area, options.DryRun, options.ForceNugetRefresh));
        }
    }

    /// <summary>Folds one props-file result into the run summary.</summary>
    private static void Record(
        RunSummary summary,
        string area,
        DirectoryInfo path,
        DirectoryBuildPropsResult result)
    {
        summary.Files.Add(new RunFileResult(
            area,
            DirectoryBuildPropsHelper.FileName,
            Path.Combine(path.FullName, DirectoryBuildPropsHelper.FileName),
            result.Outcome));

        foreach (var bump in result.PackageBumps)
        {
            summary.PackageBumps.Add(new RunPackageBump(
                bump.PackageId,
                bump.Version.ToString(),
                bump.NewestVersion.ToString()));
        }

        if (result.Drift.HasDrift)
        {
            summary.Drift.Add(new RunDriftEntry(
                area,
                result.Drift.PackageReferencesOnlyUpstream,
                result.Drift.PackageReferencesOnlyLocal,
                result.Drift.PropertiesOnlyUpstream));
        }
    }

    /// <summary>
    /// Yields every configured mapping as an (area, directory) pair, in the fixed
    /// sample / src / test order the output has always used.
    /// </summary>
    internal static IEnumerable<(string Area, DirectoryInfo Path)> EnumerateMappedPaths(
        OptionsFile options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return EnumerateMappedPathsIterator(options);
    }

    private static IEnumerable<(string Area, DirectoryInfo Path)> EnumerateMappedPathsIterator(
        OptionsFile options)
    {
        var areas = new (string Area, IEnumerable<string> Paths)[]
        {
            ("sample", options.Mappings.Sample.Paths),
            ("src", options.Mappings.Src.Paths),
            ("test", options.Mappings.Test.Paths),
        };

        foreach (var (area, paths) in areas)
        {
            foreach (var path in paths)
            {
                yield return (area, new DirectoryInfo(path));
            }
        }
    }

    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "OK.")]
    private static async Task HandleTemporarySuppressions(
        ILogger logger,
        DirectoryInfo projectPath,
        FileInfo? buildFile,
        DirectoryInfo? temporarySuppressionsPath,
        bool temporarySuppressionAsExcel,
        CancellationToken cancellationToken)
    {
        logger.LogInformation($"{AppEmojisConstants.AreaTemporarySuppression} Working on temporary suppressions");

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
            buildResult = await DotnetBuildHelper.BuildAndCollectErrors(
                logger,
                projectPath,
                1,
                buildFile,
                useNugetRestore: true,
                useConfigurationReleaseMode: true,
                BuildDefaultTimeoutInSec,
                "     ",
                cancellationToken);
        }
        catch (DataException ex)
        {
            logger.LogError($"{EmojisConstants.Error} {ex.Message}");
            return;
        }
        catch (IOException ex)
        {
            logger.LogError($"{EmojisConstants.Error} {ex.Message}");
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
                        2 + i,
                        buildFile,
                        buildResult,
                        analyzerProviderBaseRules,
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

    private static async Task<bool> BuildAndCollectErrorsAgainAndUpdateFile(
        ILogger logger,
        DirectoryInfo projectPath,
        int runNumber,
        FileInfo? buildFile,
        Dictionary<string, int> buildResult,
        Collection<AnalyzerProviderBaseRuleData> analyzerProviderBaseRules,
        CancellationToken cancellationToken)
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
                "     ",
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
            logger.LogError($"{EmojisConstants.Error} {ex.Message}");
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
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

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

        await Helpers.FileHelper.WriteAllTextAsync(new FileInfo(temporarySuppressionsFile), suppressionsText);
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

    private static string CreateSuppressionsText(
        IEnumerable<Tuple<string, List<string>>> suppressionLinesPrAnalyzer)
    {
        var sb = new StringBuilder();
        sb.AppendLine(GlobalizationConstants.EnglishCultureInfo, $"{EditorConfigHelper.AutogeneratedCustomSectionHeaderPrefix} {DateTime.Now:F}");
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