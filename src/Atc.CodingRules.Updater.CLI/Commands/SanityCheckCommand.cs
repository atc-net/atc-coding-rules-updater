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

    private async Task<int> ExecuteInternalAsync(
        SanityCheckCommandSettings settings,
        CancellationToken cancellationToken)
    {
        var jsonOutput = settings.OutputJson.GetValueOrDefault();
        if (!jsonOutput)
        {
            ConsoleHelper.WriteHeader();
        }

        var projectPath = new DirectoryInfo(settings.ProjectPath);
        var options = await GetOptionsFromFileAndUserArguments(settings, projectPath, cancellationToken);

        if (jsonOutput)
        {
            var diagnostics = ProjectSanityCheckHelper.CheckFilesAndCollect(projectPath, options.ProjectTarget);
            WriteJsonSummary(diagnostics);
            return diagnostics.Any(d => d.Severity == SanityCheckSeverity.Error)
                ? ConsoleExitStatusCodes.Failure
                : ConsoleExitStatusCodes.Success;
        }

        try
        {
            await ProjectHelper.SanityCheckFiles(logger, projectPath, options);
        }
        catch (Exception ex)
        {
            logger.LogError($"{EmojisConstants.Error} {ex.Message}");
            return ConsoleExitStatusCodes.Failure;
        }

        logger.LogInformation($"{EmojisConstants.Success} Done");
        return ConsoleExitStatusCodes.Success;
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