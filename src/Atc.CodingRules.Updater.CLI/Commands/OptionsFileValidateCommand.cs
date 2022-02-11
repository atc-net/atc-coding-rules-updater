namespace Atc.CodingRules.Updater.CLI.Commands;

public class OptionsFileValidateCommand : AsyncCommand<ProjectBaseCommandSettings>
{
    private readonly ILogger<OptionsFileValidateCommand> logger;

    public OptionsFileValidateCommand(ILogger<OptionsFileValidateCommand> logger) => this.logger = logger;

    public override Task<int> ExecuteAsync(
        CommandContext context,
        ProjectBaseCommandSettings settings)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(settings);
        return ExecuteInternalAsync(settings);
    }

    private async Task<int> ExecuteInternalAsync(
        ProjectBaseCommandSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ConsoleHelper.WriteHeader();

        var projectPath = new DirectoryInfo(settings.ProjectPath);
        var optionsPath = settings.GetOptionsPath();

        try
        {
            (bool isSuccessful, string error) = await OptionsHelper.ValidateOptionsFile(projectPath, optionsPath);
            if (isSuccessful)
            {
                logger.LogInformation("The options file is valid");
            }
            else
            {
                logger.LogError(error);
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"{Console.Spectre.EmojisConstants.Error} {ex.GetMessage()}");
            return ConsoleExitStatusCodes.Failure;
        }

        logger.LogInformation($"{Console.Spectre.EmojisConstants.Done} Done");
        return ConsoleExitStatusCodes.Success;
    }
}