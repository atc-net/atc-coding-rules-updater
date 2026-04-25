namespace Atc.CodingRules.Updater.CLI.Commands;

public class SanityCheckCommand(ILogger<SanityCheckCommand> logger) : AsyncCommand<ProjectCommandSettings>
{
    protected override Task<int> ExecuteAsync(
        CommandContext context,
        ProjectCommandSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(settings);
        return ExecuteInternalAsync(settings, cancellationToken);
    }

    private async Task<int> ExecuteInternalAsync(
        ProjectCommandSettings settings,
        CancellationToken cancellationToken)
    {
        ConsoleHelper.WriteHeader();

        var projectPath = new DirectoryInfo(settings.ProjectPath);
        var options = await GetOptionsFromFileAndUserArguments(settings, projectPath, cancellationToken);

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
        ProjectCommandSettings settings,
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
}