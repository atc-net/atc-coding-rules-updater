namespace Atc.CodingRules.Updater.CLI.Commands;

public class AnalyzerProvidersCollectCommand(ILogger<AnalyzerProvidersCollectCommand> logger)
    : AsyncCommand<AnalyzerProvidersCollectCommandSettings>
{
    protected override Task<int> ExecuteAsync(
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
        var jsonOutput = settings.OutputJson.GetValueOrDefault();
        if (!jsonOutput)
        {
            ConsoleHelper.WriteHeader();
        }

        var projectPath = new DirectoryInfo(settings.ProjectPath);
        var options = await GetOptionsFromFileAndUserArguments(settings, projectPath, cancellationToken);

        var includeProviders = SplitProviderList(settings.IncludeProviders);
        var excludeProviders = SplitProviderList(settings.ExcludeProviders);

        Collection<AnalyzerProviderBaseRuleData> result;
        try
        {
            if (!jsonOutput)
            {
                logger.LogInformation("Working on analyzer providers collect base rules metadata");
            }

            // In --json mode stdout must contain nothing but the JSON document. The console
            // logger writes to stdout too, so collection progress is discarded rather than
            // interleaved; per-provider failures are not lost, they are reported through the
            // ExceptionMessage field of the JSON summary below.
            result = await AnalyzerProviderBaseRulesHelper.GetAnalyzerProviderBaseRules(
                jsonOutput ? NullLogger.Instance : logger,
                options.AnalyzerProviderCollectingMode,
                logWithAnsiConsoleMarkup: !jsonOutput,
                includeProviders,
                excludeProviders);
        }
        catch (Exception ex)
        {
            logger.LogError($"{EmojisConstants.Error} {ex.GetMessage()}");
            return ConsoleExitStatusCodes.Failure;
        }

        if (jsonOutput)
        {
            WriteJsonSummary(result);
        }
        else
        {
            logger.LogInformation($"{EmojisConstants.Success} Done");
        }

        return ConsoleExitStatusCodes.Success;
    }

    private static void WriteJsonSummary(
        IEnumerable<AnalyzerProviderBaseRuleData> result)
    {
        var summary = result
            .Select(p => new
            {
                p.Name,
                RuleCount = p.Rules.Count,
                p.ExceptionMessage,
            })
            .ToArray();

        var json = JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true });
        System.Console.Out.WriteLine(json);
    }

    private static IReadOnlyCollection<string>? SplitProviderList(
        FlagValue<string> flag)
    {
        if (!flag.IsSet || string.IsNullOrWhiteSpace(flag.Value))
        {
            return null;
        }

        return flag.Value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();
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