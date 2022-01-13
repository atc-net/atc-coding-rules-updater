namespace Atc.CodingRules.Updater.CLI.Commands;

public class AnalyzerProvidersCacheCleanupCommand : Command
{
    private readonly ILogger<AnalyzerProvidersCacheCleanupCommand> logger;

    public AnalyzerProvidersCacheCleanupCommand(ILogger<AnalyzerProvidersCacheCleanupCommand> logger) => this.logger = logger;

    public override int Execute(CommandContext context)
    {
        ConsoleHelper.WriteHeader();

        try
        {
            logger.LogInformation("Working on analyzer providers cache cleanup");
            AnalyzerProviderBaseRulesHelper.CleanupCache(logger);
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