// ReSharper disable SuggestBaseTypeForParameter
namespace Atc.CodingRules.Updater.CLI.Commands;

[SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "OK.")]
public class RunCommand(ILogger<RunCommand> logger) : AsyncCommand<RunCommandSettings>
{
    protected override Task<int> ExecuteAsync(
        CommandContext context,
        RunCommandSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(settings);
        return ExecuteInternalAsync(settings, cancellationToken);
    }

    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "Linear argument validation then a single run; splitting hurts readability.")]
    internal async Task<int> ExecuteInternalAsync(
        RunCommandSettings settings,
        CancellationToken cancellationToken)
    {
        // Validate the arguments before probing the network, so a typo fails fast and with a
        // message about the typo rather than about connectivity.
        var projectPath = ProjectHelper.GetExistingProjectPath(logger, settings.ProjectPath);
        if (projectPath is null)
        {
            return ConsoleExitStatusCodes.Failure;
        }

        if (!await NetworkInformationHelper.HasHttpConnectionAsync(cancellationToken))
        {
            System.Console.WriteLine("This tool requires internet connection!");
            return ConsoleExitStatusCodes.Failure;
        }

        var jsonOutput = settings.OutputJson.GetValueOrDefault();
        if (!jsonOutput)
        {
            ConsoleHelper.WriteHeader();
        }

        var options = await GetOptionsFromFileAndUserArguments(settings, projectPath, cancellationToken);

        // The console logger writes to stdout, so in --json mode the run is silenced to keep the
        // document parseable. This is the same defect that made 'analyzer-providers collect
        // --json' unusable.
        ILogger runLogger = jsonOutput ? NullLogger.Instance : logger;

        RunSummary summary;
        try
        {
            if (!jsonOutput)
            {
                CodingRulesUpdaterVersionHelper.PrintUpdateInfoIfNeeded(logger);
            }

            summary = await ProjectHelper.HandleFiles(
                runLogger,
                projectPath,
                options,
                cancellationToken);

            var placeholderExit = await ResolvePlaceholdersAsync(settings, projectPath, runLogger, jsonOutput, cancellationToken);
            if (placeholderExit is not null)
            {
                return placeholderExit.Value;
            }
        }
        catch (Exception ex)
        {
            if (jsonOutput)
            {
                WriteJson(new { Error = ex.Message });
            }
            else
            {
                logger.LogError($"{EmojisConstants.Error} {Markup.Escape(ex.Message)}");
            }

            return ConsoleExitStatusCodes.Failure;
        }

        if (jsonOutput)
        {
            WriteJson(summary);
        }
        else
        {
            logger.LogInformation($"{EmojisConstants.Success} Done");
        }

        return MapExitCode(summary, settings.FailOnChanges.GetValueOrDefault());
    }

    /// <summary>
    /// Maps a completed run to an exit code. Only <c>--failOnChanges</c> can turn a successful run
    /// into a failure, and only when something was actually created or updated.
    /// </summary>
    internal static int MapExitCode(
        RunSummary summary,
        bool failOnChanges)
    {
        ArgumentNullException.ThrowIfNull(summary);

        return failOnChanges && summary.HasChanges
            ? ConsoleExitStatusCodes.Failure
            : ConsoleExitStatusCodes.Success;
    }

    /// <summary>
    /// Substitutes the <c>OrganizationName</c> / <c>RepositoryName</c> placeholders, prompting only
    /// when there is a human to answer. Returns a non-null exit code when the run cannot continue.
    /// </summary>
    private static async Task<int?> ResolvePlaceholdersAsync(
        RunCommandSettings settings,
        DirectoryInfo projectPath,
        ILogger runLogger,
        bool jsonOutput,
        CancellationToken cancellationToken)
    {
        var placeholders = new[]
        {
            ("OrganizationName", "insert organization name here", settings.OrganizationName, "Organization name"),
            ("RepositoryName", "insert repository name here", settings.RepositoryName, "Repository name"),
        };

        foreach (var (element, placeholder, flag, prompt) in placeholders)
        {
            if (!DirectoryBuildPropsHelper.HasFileInsertPlaceholderElement(projectPath, element, placeholder))
            {
                continue;
            }

            string value;
            if (flag is not null && flag.IsSet)
            {
                value = flag.Value;
            }
            else if (jsonOutput)
            {
                // Prompting would hang a CI job waiting on stdin.
                WriteJson(new { Error = $"{element} is not set and cannot be prompted for in --json mode. Pass --{char.ToLowerInvariant(element[0])}{element[1..]}." });
                return ConsoleExitStatusCodes.Failure;
            }
            else
            {
                value = await AnsiConsole.AskAsync<string>($"What is the [green]{prompt}[/]?", cancellationToken);
            }

            DirectoryBuildPropsHelper.UpdateFileInsertPlaceholderElement(runLogger, projectPath, element, placeholder, value);
        }

        return null;
    }

    private static void WriteJson(object value)
        => System.Console.Out.WriteLine(
            JsonSerializer.Serialize(
                value,
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters = { new JsonStringEnumConverter() },
                }));

    private static async Task<OptionsFile> GetOptionsFromFileAndUserArguments(
        RunCommandSettings settings,
        DirectoryInfo projectPath,
        CancellationToken cancellationToken)
    {
        var optionsPath = settings.GetOptionsPath();
        var options = await OptionsHelper.CreateDefault(projectPath, optionsPath, cancellationToken);
        options.Mappings.ResolvePaths(projectPath);

        var projectTarget = ProjectCommandSettings.GetProjectTarget(settings);
        if (projectTarget is not null)
        {
            options.ProjectTarget = (SupportedProjectTargetType)projectTarget;
        }

        if (settings.UseLatestMinorNugetVersion.HasValue)
        {
            options.UseLatestMinorNugetVersion = settings.UseLatestMinorNugetVersion.GetValueOrDefault();
        }

        if (settings.UseTemporarySuppressions.HasValue)
        {
            options.UseTemporarySuppressions = settings.UseTemporarySuppressions.GetValueOrDefault();
        }

        var temporarySuppressionsPath = GetTemporarySuppressionsPath(settings);
        if (temporarySuppressionsPath is not null &&
            temporarySuppressionsPath.Exists)
        {
            options.TemporarySuppressionsPath = temporarySuppressionsPath.FullName;
        }

        if (settings.TemporarySuppressionAsExcel.HasValue)
        {
            options.TemporarySuppressionAsExcel = settings.TemporarySuppressionAsExcel.GetValueOrDefault();
        }

        if (settings.DryRun.HasValue)
        {
            options.DryRun = settings.DryRun.GetValueOrDefault();
        }

        if (settings.BuildConfiguration is not null && settings.BuildConfiguration.IsSet)
        {
            options.BuildConfiguration = settings.BuildConfiguration.Value;
        }

        if (settings.BuildProperties is { Length: > 0 })
        {
            options.BuildProperties = settings.BuildProperties;
        }

        if (settings.ForceNugetRefresh.HasValue)
        {
            options.ForceNugetRefresh = settings.ForceNugetRefresh.GetValueOrDefault();
        }

        var buildFile = GetBuildFile(settings, projectPath);
        if (buildFile is not null)
        {
            options.BuildFile = buildFile.FullName;
        }

        return options;
    }

    private static DirectoryInfo? GetTemporarySuppressionsPath(
        RunCommandSettings settings)
    {
        var temporarySuppressionsPath = string.Empty;
        if (settings.TemporarySuppressionsPath is not null && settings.TemporarySuppressionsPath.IsSet)
        {
            temporarySuppressionsPath = settings.TemporarySuppressionsPath.Value;
        }

        return !string.IsNullOrEmpty(temporarySuppressionsPath)
            ? new DirectoryInfo(temporarySuppressionsPath)
            : null;
    }

    internal static FileInfo? GetBuildFile(
        RunCommandSettings settings,
        DirectoryInfo projectPath)
    {
        var buildFile = string.Empty;
        if (settings.BuildFile is not null && settings.BuildFile.IsSet)
        {
            buildFile = settings.BuildFile.Value;
        }

        // Path.Combine returns its second argument unchanged when that argument is rooted, so it
        // handles absolute and relative --buildFile values on every platform. The colon check this
        // replaced was redundant, and sent relative paths containing a colon (legal on Unix) to the
        // current working directory instead of --projectPath.
        return string.IsNullOrEmpty(buildFile)
            ? null
            : new FileInfo(Path.Combine(projectPath.FullName, buildFile));
    }
}