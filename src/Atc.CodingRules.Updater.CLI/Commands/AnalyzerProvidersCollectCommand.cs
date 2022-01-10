namespace Atc.CodingRules.Updater.CLI.Commands
{
    public class AnalyzerProvidersCollectCommand : AsyncCommand<AnalyzerProvidersCollectCommandSettings>
    {
        private readonly ILogger<AnalyzerProvidersCollectCommand> logger;

        public AnalyzerProvidersCollectCommand(ILogger<AnalyzerProvidersCollectCommand> logger) => this.logger = logger;

        public override Task<int> ExecuteAsync(
            CommandContext context,
            AnalyzerProvidersCollectCommandSettings settings)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(settings);
            return ExecuteInternalAsync(settings);
        }

        private async Task<int> ExecuteInternalAsync(
            AnalyzerProvidersCollectCommandSettings settings)
        {
            ConsoleHelper.WriteHeader();

            var outputRootPath = new DirectoryInfo(settings.OutputRootPath);
            var optionsPath = settings.GetOptionsPath();
            var options = await OptionsHelper.CreateDefault(outputRootPath, optionsPath);

            var analyzerProviderCollectingMode = GetAnalyzerProviderCollectingMode(settings);
            if (analyzerProviderCollectingMode is not null)
            {
                options.AnalyzerProviderCollectingMode = (ProviderCollectingMode)analyzerProviderCollectingMode;
            }

            try
            {
                logger.LogInformation("Working on analyzer providers collect base rules metadata");
                await AnalyzerProviderBaseRulesHelper.GetAnalyzerProviderBaseRules(
                    logger,
                    options.AnalyzerProviderCollectingMode,
                    logWithAnsiConsoleMarkup: true);
            }
            catch (Exception ex)
            {
                logger.LogError($"{EmojisConstants.Error} {ex.GetMessage()}");
                return ConsoleExitStatusCodes.Failure;
            }

            logger.LogInformation($"{EmojisConstants.Done} Done");
            return ConsoleExitStatusCodes.Success;
        }

        private static ProviderCollectingMode? GetAnalyzerProviderCollectingMode(
            AnalyzerProvidersCollectCommandSettings settings)
            => settings.CollectingMode.IsSet
                ? settings.CollectingMode.Value
                : null;
    }
}