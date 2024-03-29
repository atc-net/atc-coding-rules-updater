namespace Atc.CodingRules.Updater.CLI.Commands;

[SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "OK.")]
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

        logger.LogInformation($"{EmojisConstants.Success} Done");
        return ConsoleExitStatusCodes.Success;
    }
}