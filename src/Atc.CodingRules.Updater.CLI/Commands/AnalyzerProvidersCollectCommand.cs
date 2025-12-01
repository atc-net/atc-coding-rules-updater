namespace Atc.CodingRules.Updater.CLI.Commands;

public class AnalyzerProvidersCollectCommand(ILogger<AnalyzerProvidersCollectCommand> logger)
    : AsyncCommand<AnalyzerProvidersCollectCommandSettings>
{
    public override Task<int> ExecuteAsync(
        CommandContext context,
        AnalyzerProvidersCollectCommandSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(settings);
        return ExecuteInternalAsync(settings, cancellationToken);
    }

    private async Task<int> ExecuteInternalAsync(
        AnalyzerProvidersCollectCommandSettings settings,
        CancellationToken cancellationToken)
    {
        ConsoleHelper.WriteHeader();

        var projectPath = new DirectoryInfo(settings.ProjectPath);
        var options = await GetOptionsFromFileAndUserArguments(settings, projectPath, cancellationToken);

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

        logger.LogInformation($"{EmojisConstants.Success} Done");
        return ConsoleExitStatusCodes.Success;
    }

    private static async Task<OptionsFile> GetOptionsFromFileAndUserArguments(
        AnalyzerProvidersCollectCommandSettings settings,
        DirectoryInfo projectPath,
        CancellationToken cancellationToken)
    {
        var optionsPath = settings.GetOptionsPath();
        var options = await OptionsHelper.CreateDefault(projectPath, optionsPath, cancellationToken);

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