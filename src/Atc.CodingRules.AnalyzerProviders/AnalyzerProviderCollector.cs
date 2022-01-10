namespace Atc.CodingRules.AnalyzerProviders;

public class AnalyzerProviderCollector
{
    private readonly ILogger logger;

    public AnalyzerProviderCollector(ILogger logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public static string[] GetAllBaseRuleProviderNames()
    {
        var list = new List<string>
        {
            AsyncFixerProvider.Name,
            AsyncifyProvider.Name,
            MeziantouProvider.Name,
            MicrosoftCodeAnalysisNetAnalyzersProvider.Name,
            MicrosoftCompilerErrorsProvider.Name,
            MicrosoftCompilerErrorsProviderUndocumented.Name,
            SecurityCodeScanVs2019Provider.Name,
            StyleCopAnalyzersProvider.Name,
            SonarAnalyzerCSharpProvider.Name,
        };

        return list.ToArray();
    }

    public async Task<Collection<AnalyzerProviderBaseRuleData>> CollectAllBaseRules(
        ProviderCollectingMode providerCollectingMode)
    {
        var data = new Collection<AnalyzerProviderBaseRuleData>();

        var asyncFixerProvider = new AsyncFixerProvider(logger);
        var asyncFixerTask = asyncFixerProvider.CollectBaseRules(providerCollectingMode);

        var asyncifyProvider = new AsyncifyProvider(logger);
        var asyncifyTask = asyncifyProvider.CollectBaseRules(providerCollectingMode);

        var meziantouProvider = new MeziantouProvider(logger);
        var meziantouTask = meziantouProvider.CollectBaseRules(providerCollectingMode);

        var microsoftCodeAnalysisNetAnalyzersProvider = new MicrosoftCodeAnalysisNetAnalyzersProvider(logger);
        var microsoftCodeAnalysisNetAnalyzersTask = microsoftCodeAnalysisNetAnalyzersProvider.CollectBaseRules(providerCollectingMode);

        var microsoftCompilerErrorsProvider = new MicrosoftCompilerErrorsProvider(logger);
        var microsoftCompilerErrorsTask = microsoftCompilerErrorsProvider.CollectBaseRules(providerCollectingMode);

        var microsoftCodeAnalysisNetAnalyzersProviderUndocumented = new MicrosoftCompilerErrorsProviderUndocumented(logger);
        var microsoftCompilerErrorsUndocumentedTask = microsoftCodeAnalysisNetAnalyzersProviderUndocumented.CollectBaseRules(ProviderCollectingMode.ReCollect);

        var securityCodeScanVs2019Provider = new SecurityCodeScanVs2019Provider(logger);
        var securityCodeScanVs2019Task = securityCodeScanVs2019Provider.CollectBaseRules(providerCollectingMode);

        var styleCopAnalyzersProvider = new StyleCopAnalyzersProvider(logger);
        var styleCopAnalyzersTask = styleCopAnalyzersProvider.CollectBaseRules(providerCollectingMode);

        var sonarAnalyzerCSharpProvider = new SonarAnalyzerCSharpProvider(logger);
        var sonarAnalyzerCSharpTask = sonarAnalyzerCSharpProvider.CollectBaseRules(providerCollectingMode);

        await Task.WhenAll(
            asyncFixerTask,
            asyncifyTask,
            meziantouTask,
            microsoftCodeAnalysisNetAnalyzersTask,
            microsoftCompilerErrorsTask,
            microsoftCompilerErrorsUndocumentedTask,
            securityCodeScanVs2019Task,
            styleCopAnalyzersTask,
            sonarAnalyzerCSharpTask);

        data.Add(await asyncFixerTask);
        data.Add(await asyncifyTask);
        data.Add(await meziantouTask);
        data.Add(await microsoftCodeAnalysisNetAnalyzersTask);
        data.Add(await microsoftCompilerErrorsTask);
        data.Add(await microsoftCompilerErrorsUndocumentedTask);
        data.Add(await securityCodeScanVs2019Task);
        data.Add(await styleCopAnalyzersTask);
        data.Add(await sonarAnalyzerCSharpTask);

        return data;
    }

    public void CacheCleanup()
    {
        var asyncFixerProvider = new AsyncFixerProvider(logger);
        asyncFixerProvider.Cleanup();

        var asyncifyProvider = new AsyncifyProvider(logger);
        asyncifyProvider.Cleanup();

        var meziantouProvider = new MeziantouProvider(logger);
        meziantouProvider.Cleanup();

        var microsoftCodeAnalysisNetAnalyzersProvider = new MicrosoftCodeAnalysisNetAnalyzersProvider(logger);
        microsoftCodeAnalysisNetAnalyzersProvider.Cleanup();

        var microsoftCompilerErrorsProvider = new MicrosoftCompilerErrorsProvider(logger);
        microsoftCompilerErrorsProvider.Cleanup();

        var microsoftCodeAnalysisNetAnalyzersProviderUndocumented = new MicrosoftCompilerErrorsProviderUndocumented(logger);
        microsoftCodeAnalysisNetAnalyzersProviderUndocumented.Cleanup();

        var securityCodeScanVs2019Provider = new SecurityCodeScanVs2019Provider(logger);
        securityCodeScanVs2019Provider.Cleanup();

        var styleCopAnalyzersProvider = new StyleCopAnalyzersProvider(logger);
        styleCopAnalyzersProvider.Cleanup();

        var sonarAnalyzerCSharpProvider = new SonarAnalyzerCSharpProvider(logger);
        sonarAnalyzerCSharpProvider.Cleanup();
    }
}