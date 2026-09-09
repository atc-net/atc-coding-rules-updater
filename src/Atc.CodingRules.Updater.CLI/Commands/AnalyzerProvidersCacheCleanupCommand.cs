namespace Atc.CodingRules.Updater.CLI.Commands;

public class AnalyzerProvidersCacheCleanupCommand(ILogger<AnalyzerProvidersCacheCleanupCommand> logger) : Command
{
    protected override int Execute(
        CommandContext context,
        CancellationToken cancellationToken)
    {
        ConsoleHelper.WriteHeader();

        try
        {
            logger.LogInformation("Working on analyzer providers cache cleanup");
            AnalyzerProviderBaseRulesHelper.CleanupCache(logger);
        }
        catch (Exception ex)
        {
            logger.LogError($"{EmojisConstants.Error} {Markup.Escape(ex.GetMessage())}");
            return ConsoleExitStatusCodes.Failure;
        }

        logger.LogInformation($"{EmojisConstants.Success} Done");
        return ConsoleExitStatusCodes.Success;
    }
}