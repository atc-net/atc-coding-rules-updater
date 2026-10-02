namespace Atc.CodingRules.Updater.CLI.Helpers;

/// <summary>
/// The work shared by <c>cpm validate</c> and <c>cpm migrate</c>: building the migration options,
/// running <see cref="CpmMigrationExecutor"/>, and reporting the result as log output or JSON.
/// </summary>
public static class CpmCommandHelper
{
    /// <summary>
    /// Runs the migration (or, for <c>cpm validate</c>, only plans it) and returns the exit code.
    /// </summary>
    internal static async Task<int> ExecuteAsync(
        ILogger logger,
        CpmCommandSettings settings,
        ICpmMigrationEnvironment environment,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var jsonOutput = settings.OutputJson.GetValueOrDefault();
        var runLogger = jsonOutput ? NullLogger.Instance : logger;
        if (!jsonOutput)
        {
            ConsoleHelper.WriteHeader();
        }

        var projectPath = ProjectHelper.GetExistingProjectPath(runLogger, settings.ProjectPath);
        if (projectPath is null)
        {
            return ConsoleExitStatusCodes.Failure;
        }

        var options = await CreateOptions(settings, projectPath, cancellationToken);
        var result = await CpmMigrationExecutor.ExecuteAsync(runLogger, projectPath, options, environment, cancellationToken);

        if (jsonOutput)
        {
            await System.Console.Out.WriteLineAsync(SerializeJson(result));
        }
        else
        {
            LogResult(runLogger, projectPath, result);
        }

        return MapExitCode(result.Outcome);
    }

    internal static int MapExitCode(CpmMigrationOutcome outcome)
        => outcome is CpmMigrationOutcome.Blocked or CpmMigrationOutcome.RolledBack
            ? ConsoleExitStatusCodes.Failure
            : ConsoleExitStatusCodes.Success;

    /// <summary>
    /// Combines the command-line flags with the options file. <c>buildFile</c> and
    /// <c>buildConfiguration</c> are shared with <c>run</c>, so a repository configures them once.
    /// Settings that are not <see cref="CpmMigrateCommandSettings"/> come from
    /// <c>cpm validate</c>, which always only plans.
    /// </summary>
    internal static async Task<CpmMigrationOptions> CreateOptions(
        CpmCommandSettings settings,
        DirectoryInfo projectPath,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(projectPath);

        var optionsFile = await OptionsHelper.CreateDefault(projectPath, settings.GetOptionsPath(), cancellationToken);

        var buildFile = settings.BuildFile is { IsSet: true } ? settings.BuildFile.Value : optionsFile.BuildFile;
        var buildConfiguration = settings.BuildConfiguration is { IsSet: true } ? settings.BuildConfiguration.Value : optionsFile.BuildConfiguration;
        var migrateSettings = settings as CpmMigrateCommandSettings;

        return new CpmMigrationOptions
        {
            DryRun = migrateSettings is null || migrateSettings.DryRun.GetValueOrDefault(),
            UnifyVersions = settings.UnifyVersions.GetValueOrDefault(),
            SkipRestoreVerification = migrateSettings?.SkipRestoreVerification.GetValueOrDefault() ?? false,
            AllowDirty = migrateSettings?.AllowDirty.GetValueOrDefault() ?? false,
            RestoreTarget = string.IsNullOrWhiteSpace(buildFile)
                ? null
                : Path.GetFullPath(buildFile, projectPath.FullName),
            BuildConfiguration = string.IsNullOrWhiteSpace(buildConfiguration) ? "Release" : buildConfiguration,
        };
    }

    internal static string SerializeJson(CpmMigrationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return RunCommand.SerializeJson(new
        {
            result.Outcome,
            result.Plan.Blockers,
            result.Plan.Packages,
            result.Plan.Overrides,
            ChangedFiles = result.Plan.FileChanges.Keys.Order(StringComparer.Ordinal).ToArray(),
            result.Differences,
        });
    }

    private static void LogResult(
        ILogger logger,
        DirectoryInfo projectPath,
        CpmMigrationResult result)
    {
        var plan = result.Plan;
        switch (result.Outcome)
        {
            case CpmMigrationOutcome.AlreadyMigrated:
                logger.LogInformation($"{EmojisConstants.Success} The repository already uses Central Package Management - nothing to do");
                return;
            case CpmMigrationOutcome.NothingToMigrate:
                logger.LogInformation($"{EmojisConstants.Success} No versioned PackageReference items found - nothing to migrate");
                return;
            case CpmMigrationOutcome.Blocked:
                logger.LogError($"{EmojisConstants.Error} The migration cannot start - fix the following first:");
                foreach (var blocker in plan.Blockers)
                {
                    var location = blocker.FilePath is null ? string.Empty : $" ({blocker.FilePath})";
                    logger.LogError($"     {Markup.Escape($"[{blocker.Code}] {blocker.Message}{location}")}");
                }

                return;
        }

        var globals = plan.Packages.Count(x => x.ItemType == PackageItemType.GlobalPackageReference);
        logger.LogInformation($"{AppEmojisConstants.PackageReference}   {plan.Packages.Count - globals} PackageVersion and {globals} GlobalPackageReference entries in {CentralPackageManagementHelper.PackagesPropsFileName}");
        foreach (var file in plan.FileChanges.Keys.Order(StringComparer.Ordinal))
        {
            logger.LogInformation($"     {Markup.Escape(Path.GetRelativePath(projectPath.FullName, file))}");
        }

        foreach (var versionOverride in plan.Overrides)
        {
            logger.LogWarning($"     {versionOverride.Id} keeps {versionOverride.Version} through VersionOverride in {Markup.Escape(Path.GetRelativePath(projectPath.FullName, versionOverride.FilePath))} - consider unifying");
        }

        switch (result.Outcome)
        {
            case CpmMigrationOutcome.DryRun:
                logger.LogInformation($"[dim](dry-run)[/] nothing was written - run 'atc-coding-rules-updater {NameCommandConstants.Cpm} {NameCommandConstants.CpmMigrate}' to apply it");
                break;
            case CpmMigrationOutcome.Migrated:
                logger.LogInformation($"{EmojisConstants.Success} Migrated. Review and commit the changes, then run 'atc-coding-rules-updater run', which now detects Central Package Management. Consider enabling CentralPackageTransitivePinningEnabled afterwards.");
                break;
            case CpmMigrationOutcome.RolledBack:
                logger.LogError($"{EmojisConstants.Error} Restore changed after the migration, so every file was put back:");
                foreach (var difference in result.Differences)
                {
                    logger.LogError($"     {Markup.Escape(difference)}");
                }

                break;
        }
    }
}