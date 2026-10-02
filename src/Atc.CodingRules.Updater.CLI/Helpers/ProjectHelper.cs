// ReSharper disable SwitchStatementMissingSomeEnumCasesNoDefault
// ReSharper disable InvertIf
// ReSharper disable SuggestBaseTypeForParameter
namespace Atc.CodingRules.Updater.CLI.Helpers;

public static class ProjectHelper
{
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
            options.ProjectTarget,
            options.CentralPackageManagement);

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
                await TemporarySuppressionHelper.HandleAsync(logger, projectPath, options, cancellationToken);
            }
        }

        return summary;
    }

    /// <summary>
    /// Resolves <paramref name="projectPath"/> to an existing directory, or logs a clear error and
    /// returns <c>null</c>. Without this a mistyped <c>--project-path</c> surfaces as an unhandled
    /// <see cref="DirectoryNotFoundException"/> from deep inside the options loader.
    /// </summary>
    internal static DirectoryInfo? GetExistingProjectPath(
        ILogger logger,
        string projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            logger.LogError($"{EmojisConstants.Error} No project path was given - use --project-path");
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
            options.ProjectTarget,
            options.CentralPackageManagement);
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

        var centralPackageManagement = CentralPackageManagementHelper.Detect(projectPath, options.CentralPackageManagement);
        if (centralPackageManagement.IsEnabled)
        {
            CentralPackageManagementRunHelper.HandleDirectoryBuildPropsFiles(
                logger,
                projectPath,
                options,
                summary,
                rawCodingRulesDistributionProjectTargetBaseUrl,
                centralPackageManagement);
            return;
        }

        // Console only: the summary and its JSON stay exactly as before for these repositories.
        logger.LogInformation($"{AppEmojisConstants.AreaDirectoryBuildProps} Tip: 'atc-coding-rules-updater {NameCommandConstants.Cpm} {NameCommandConstants.CpmMigrate}' can move package versions into {CentralPackageManagementHelper.PackagesPropsFileName} (Central Package Management)");

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
    internal static void Record(
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
}