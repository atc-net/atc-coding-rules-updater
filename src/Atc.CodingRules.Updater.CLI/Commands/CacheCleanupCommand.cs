namespace Atc.CodingRules.Updater.CLI.Commands
{
    public class CacheCleanupCommand : Command
    {
        private readonly ILogger<CacheCleanupCommand> logger;

        public CacheCleanupCommand(ILogger<CacheCleanupCommand> logger) => this.logger = logger;

        public override int Execute(CommandContext context)
        {
            ConsoleHelper.WriteHeader();

            try
            {
                logger.LogInformation("Working on cache cleanup");
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
}