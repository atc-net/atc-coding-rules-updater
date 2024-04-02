namespace Atc.CodingRules.Updater.CLI.Commands;

public class SanityCheckCommand : AsyncCommand<ProjectCommandSettings>
{
    private readonly ILogger<SanityCheckCommand> logger;

    public SanityCheckCommand(
        ILogger<SanityCheckCommand> logger)
        => this.logger = logger;

    public override Task<int> ExecuteAsync(
        CommandContext context,
        ProjectCommandSettings settings)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(settings);
        return ExecuteInternalAsync(settings);
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "OK.")]
    private async Task<int> ExecuteInternalAsync(
        ProjectCommandSettings settings)
    {
        ConsoleHelper.WriteHeader();

        var projectPath = new DirectoryInfo(settings.ProjectPath);
        var options = await GetOptionsFromFileAndUserArguments(settings, projectPath);

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
        DirectoryInfo projectPath)
    {
        var optionsPath = settings.GetOptionsPath();
        var options = await OptionsHelper.CreateDefault(projectPath, optionsPath);
        options.Mappings.ResolvePaths(projectPath);

        var projectTarget = ProjectCommandSettings.GetProjectTarget(settings);
        if (projectTarget is not null)
        {
            options.ProjectTarget = (SupportedProjectTargetType)projectTarget;
        }

        return options;
    }
}