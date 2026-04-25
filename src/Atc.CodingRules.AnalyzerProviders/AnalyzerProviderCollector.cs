namespace Atc.CodingRules.AnalyzerProviders;

public class AnalyzerProviderCollector
{
    private readonly ILogger logger;

    public AnalyzerProviderCollector(ILogger logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public static string[] GetAllBaseRuleProviderNames()
        =>
        [
            AsyncFixerProvider.Name,
            AsyncifyProvider.Name,
            MeziantouProvider.Name,
            MicrosoftCodeAnalysisNetAnalyzersProvider.Name,
            MicrosoftCompilerErrorsProvider.Name,
            MicrosoftCompilerErrorsProviderUndocumented.Name,
            MicrosoftVisualStudioThreadingAnalyzersProvider.Name,
            NSubstituteAnalyzersProvider.Name,
            SecurityCodeScanVs2019Provider.Name,
            StyleCopAnalyzersProvider.Name,
            SonarAnalyzerCSharpProvider.Name,
            WpfAnalyzersProvider.Name,
            XunitProvider.Name,
        ];

    public Task<Collection<AnalyzerProviderBaseRuleData>> CollectAllBaseRules(
        ProviderCollectingMode providerCollectingMode,
        bool logWithAnsiConsoleMarkup)
        => CollectAllBaseRules(
            providerCollectingMode,
            logWithAnsiConsoleMarkup,
            includeProviders: null,
            excludeProviders: null);

    public async Task<Collection<AnalyzerProviderBaseRuleData>> CollectAllBaseRules(
        ProviderCollectingMode providerCollectingMode,
        bool logWithAnsiConsoleMarkup,
        IReadOnlyCollection<string>? includeProviders,
        IReadOnlyCollection<string>? excludeProviders)
    {
        var providers = ApplyProviderFilter(
            CreateAllProviders(logWithAnsiConsoleMarkup),
            includeProviders,
            excludeProviders);

        var tasks = providers
            .Select(p => p.CollectBaseRules(providerCollectingMode))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        var data = new Collection<AnalyzerProviderBaseRuleData>();
        foreach (var result in results)
        {
            data.Add(result);
        }

        return data;
    }

    private IReadOnlyList<AnalyzerProviderBase> ApplyProviderFilter(
        IReadOnlyList<AnalyzerProviderBase> providers,
        IReadOnlyCollection<string>? includeProviders,
        IReadOnlyCollection<string>? excludeProviders)
    {
        if ((includeProviders is null || includeProviders.Count == 0) &&
            (excludeProviders is null || excludeProviders.Count == 0))
        {
            return providers;
        }

        var allNames = providers
            .Select(GetProviderName)
            .ToArray();

        IEnumerable<AnalyzerProviderBase> filtered = providers;

        if (includeProviders is { Count: > 0 })
        {
            WarnUnknownProviders(includeProviders, allNames, "include");
            filtered = filtered.Where(p =>
                includeProviders.Any(inc => GetProviderName(p).Equals(inc, StringComparison.OrdinalIgnoreCase)));
        }

        if (excludeProviders is { Count: > 0 })
        {
            WarnUnknownProviders(excludeProviders, allNames, "exclude");
            filtered = filtered.Where(p =>
                !excludeProviders.Any(exc => GetProviderName(p).Equals(exc, StringComparison.OrdinalIgnoreCase)));
        }

        var result = filtered.ToList();
        var skipped = providers
            .Select(GetProviderName)
            .Except(result.Select(GetProviderName), StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (skipped.Count > 0)
        {
            logger.LogInformation($"     Skipping {skipped.Count} provider(s): {string.Join(", ", skipped)}");
        }

        return result;
    }

    private void WarnUnknownProviders(
        IReadOnlyCollection<string> requested,
        IReadOnlyCollection<string> known,
        string mode)
    {
        var unknown = requested
            .Where(r => !known.Any(k => k.Equals(r, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (unknown.Count > 0)
        {
            logger.LogWarning($"     Unknown provider name(s) in --{mode}Providers: {string.Join(", ", unknown)}. Known names: {string.Join(", ", known)}");
        }
    }

    private static string GetProviderName(AnalyzerProviderBase provider)
        => provider switch
        {
            AsyncFixerProvider => AsyncFixerProvider.Name,
            AsyncifyProvider => AsyncifyProvider.Name,
            MeziantouProvider => MeziantouProvider.Name,
            MicrosoftCodeAnalysisNetAnalyzersProvider => MicrosoftCodeAnalysisNetAnalyzersProvider.Name,
            MicrosoftCompilerErrorsProvider => MicrosoftCompilerErrorsProvider.Name,
            MicrosoftCompilerErrorsProviderUndocumented => MicrosoftCompilerErrorsProviderUndocumented.Name,
            MicrosoftVisualStudioThreadingAnalyzersProvider => MicrosoftVisualStudioThreadingAnalyzersProvider.Name,
            NSubstituteAnalyzersProvider => NSubstituteAnalyzersProvider.Name,
            SecurityCodeScanVs2019Provider => SecurityCodeScanVs2019Provider.Name,
            StyleCopAnalyzersProvider => StyleCopAnalyzersProvider.Name,
            SonarAnalyzerCSharpProvider => SonarAnalyzerCSharpProvider.Name,
            WpfAnalyzersProvider => WpfAnalyzersProvider.Name,
            XunitProvider => XunitProvider.Name,
            _ => provider.GetType().Name,
        };

    public void CacheCleanup()
    {
        foreach (var provider in CreateAllProviders(logWithAnsiConsoleMarkup: false))
        {
            provider.Cleanup();
        }
    }

    private IReadOnlyList<AnalyzerProviderBase> CreateAllProviders(
        bool logWithAnsiConsoleMarkup)
        =>
        [
            new AsyncFixerProvider(logger, logWithAnsiConsoleMarkup),
            new AsyncifyProvider(logger, logWithAnsiConsoleMarkup),
            new MeziantouProvider(logger, logWithAnsiConsoleMarkup),
            new MicrosoftCodeAnalysisNetAnalyzersProvider(logger, logWithAnsiConsoleMarkup),
            new MicrosoftCompilerErrorsProvider(logger, logWithAnsiConsoleMarkup),
            new MicrosoftCompilerErrorsProviderUndocumented(logger, logWithAnsiConsoleMarkup),
            new MicrosoftVisualStudioThreadingAnalyzersProvider(logger, logWithAnsiConsoleMarkup),
            new NSubstituteAnalyzersProvider(logger, logWithAnsiConsoleMarkup),
            new SecurityCodeScanVs2019Provider(logger, logWithAnsiConsoleMarkup),
            new StyleCopAnalyzersProvider(logger, logWithAnsiConsoleMarkup),
            new SonarAnalyzerCSharpProvider(logger, logWithAnsiConsoleMarkup),
            new WpfAnalyzersProvider(logger, logWithAnsiConsoleMarkup),
            new XunitProvider(logger, logWithAnsiConsoleMarkup),
        ];
}