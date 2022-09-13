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

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "OK.")]
    private async Task<int> ExecuteInternalAsync(
        ProjectBaseCommandSettings settings)
    {
        ConsoleHelper.WriteHeader();

        var projectPath = new DirectoryInfo(settings.ProjectPath);
        var optionsPath = settings.GetOptionsPath();

        try
        {
            var (isSuccessful, error) = await OptionsHelper.ValidateOptionsFile(projectPath, optionsPath);
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
            logger.LogError($"{EmojisConstants.Error} {ex.GetMessage()}");
            return ConsoleExitStatusCodes.Failure;
        }

        logger.LogInformation($"{EmojisConstants.Success} Done");
        return ConsoleExitStatusCodes.Success;
    }
}