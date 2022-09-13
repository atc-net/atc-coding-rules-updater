namespace Atc.CodingRules.Updater.CLI.Commands;

public class OptionsFileCreateCommand : AsyncCommand<ProjectCommandSettings>
{
    private readonly ILogger<OptionsFileCreateCommand> logger;

    public OptionsFileCreateCommand(ILogger<OptionsFileCreateCommand> logger) => this.logger = logger;

    public override Task<int> ExecuteAsync(
        CommandContext context,
        ProjectCommandSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return ExecuteInternalAsync(settings);
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "OK.")]
    private async Task<int> ExecuteInternalAsync(
        ProjectCommandSettings settings)
    {
        ConsoleHelper.WriteHeader();

        var projectPath = new DirectoryInfo(settings.ProjectPath);

        try
        {
            var (isSuccessful, error) = await OptionsHelper.CreateOptionsFile(projectPath, settings);
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

        logger.LogInformation($"{EmojisConstants.Success} Done");
        return ConsoleExitStatusCodes.Success;
    }
}