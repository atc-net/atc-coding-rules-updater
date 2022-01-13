namespace Atc.CodingRules.Updater.CLI.Commands;

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

        var projectPath = new DirectoryInfo(settings.ProjectPath);
        var options = await GetOptionsFromFileAndUserArguments(settings, projectPath);

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

    private static async Task<Options> GetOptionsFromFileAndUserArguments(
        AnalyzerProvidersCollectCommandSettings settings,
        DirectoryInfo projectPath)
    {
        var optionsPath = settings.GetOptionsPath();
        var options = await OptionsHelper.CreateDefault(projectPath, optionsPath);

        var analyzerProviderCollectingMode = GetAnalyzerProviderCollectingMode(settings);
        if (analyzerProviderCollectingMode is not null)
        {
            options.AnalyzerProviderCollectingMode = (ProviderCollectingMode)analyzerProviderCollectingMode;
        }

        return options;
    }

    private static ProviderCollectingMode? GetAnalyzerProviderCollectingMode(
        AnalyzerProvidersCollectCommandSettings settings)
        => settings.FetchMode.IsSet
            ? settings.FetchMode.Value
            : null;
}