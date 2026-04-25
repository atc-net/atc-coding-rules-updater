namespace Atc.CodingRules.Updater.CLI.Commands;

public class OptionsFileCreateCommand(ILogger<OptionsFileCreateCommand> logger)
    : AsyncCommand<ProjectCommandSettings>
{
    protected override Task<int> ExecuteAsync(
        CommandContext context,
        ProjectCommandSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return ExecuteInternalAsync(settings, cancellationToken);
    }

    private async Task<int> ExecuteInternalAsync(
        ProjectCommandSettings settings,
        CancellationToken cancellationToken)
    {
        ConsoleHelper.WriteHeader();

        var projectPath = new DirectoryInfo(settings.ProjectPath);

        try
        {
            var (isSuccessful, error) = await OptionsHelper.CreateOptionsFile(projectPath, settings, cancellationToken);
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