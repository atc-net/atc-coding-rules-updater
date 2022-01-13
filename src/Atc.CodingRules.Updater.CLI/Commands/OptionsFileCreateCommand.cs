namespace Atc.CodingRules.Updater.CLI.Commands;

public class OptionsFileCreateCommand : AsyncCommand<ProjectBaseCommandSettings>
{
    private readonly ILogger<OptionsFileCreateCommand> logger;

    public OptionsFileCreateCommand(ILogger<OptionsFileCreateCommand> logger) => this.logger = logger;

    public override Task<int> ExecuteAsync(
        CommandContext context,
        ProjectBaseCommandSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return ExecuteInternalAsync(settings);
    }

    private async Task<int> ExecuteInternalAsync(
        ProjectBaseCommandSettings settings)
    {
        ConsoleHelper.WriteHeader();

        var projectPath = new DirectoryInfo(settings.ProjectPath);
        var optionsPath = settings.GetOptionsPath();

        try
        {
            (bool isSuccessful, string error) = await OptionsHelper.CreateOptionsFile(projectPath, optionsPath);
            if (isSuccessful)
            {
                logger.LogInformation("The options file is created");
            }
            else
            {
                logger.LogError(error);
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"{EmojisConstants.Error} {ex.GetMessage()}");
            return ConsoleExitStatusCodes.Failure;
        }

        logger.LogInformation($"{EmojisConstants.Done} Done");
        return ConsoleExitStatusCodes.Success;
    }
}