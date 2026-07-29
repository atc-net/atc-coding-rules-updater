namespace Atc.CodingRules.Updater.CLI.Commands;

public class SanityCheckCommand(ILogger<SanityCheckCommand> logger) : AsyncCommand<SanityCheckCommandSettings>
{
    protected override Task<int> ExecuteAsync(
        CommandContext context,
        SanityCheckCommandSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(settings);
        return ExecuteInternalAsync(settings, cancellationToken);
    }

    internal async Task<int> ExecuteInternalAsync(
        SanityCheckCommandSettings settings,
        CancellationToken cancellationToken)
    {
        var jsonOutput = settings.OutputJson.GetValueOrDefault();
        if (!jsonOutput)
        {
            ConsoleHelper.WriteHeader();
        }

        var projectPath = ProjectHelper.GetExistingProjectPath(logger, settings.ProjectPath);
        if (projectPath is null)
        {
            return ConsoleExitStatusCodes.Failure;
        }

        var options = await GetOptionsFromFileAndUserArguments(settings, projectPath, cancellationToken);

        IReadOnlyList<SanityCheckDiagnostic> diagnostics;
        try
        {
            diagnostics = jsonOutput
                ? ProjectSanityCheckHelper.CheckFilesAndCollect(projectPath, options.ProjectTarget)
                : ProjectHelper.SanityCheckFiles(logger, projectPath, options);
        }
        catch (Exception ex)
        {
            logger.LogError($"{EmojisConstants.Error} {ex.Message}");
            return ConsoleExitStatusCodes.Failure;
        }

        // Both output modes derive the exit code from the same expression; keeping them apart
        // is what let the text mode silently return Success while --json returned Failure.
        var hasErrors = diagnostics.Any(d => d.Severity == SanityCheckSeverity.Error);

        if (jsonOutput)
        {
            WriteJsonSummary(diagnostics);
        }
        else if (!hasErrors)
        {
            logger.LogInformation($"{EmojisConstants.Success} Done");
        }

        return hasErrors
            ? ConsoleExitStatusCodes.Failure
            : ConsoleExitStatusCodes.Success;
    }

    private static async Task<OptionsFile> GetOptionsFromFileAndUserArguments(
        SanityCheckCommandSettings settings,
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

        return options;
    }

    private static void WriteJsonSummary(
        IReadOnlyList<SanityCheckDiagnostic> diagnostics)
    {
        var summary = diagnostics
            .Select(d => new
            {
                Severity = d.Severity.ToString(),
                d.Code,
                d.Message,
                d.FilePath,
            })
            .ToArray();

        var json = JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true });
        System.Console.Out.WriteLine(json);
    }
}