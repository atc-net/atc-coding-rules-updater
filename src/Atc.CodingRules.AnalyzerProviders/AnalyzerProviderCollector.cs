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
            MicrosoftVisualStudioThreadingAnalyzersProvider.Name,
            NSubstituteAnalyzersProvider.Name,
            SecurityCodeScanVs2019Provider.Name,
            StyleCopAnalyzersProvider.Name,
            SonarAnalyzerCSharpProvider.Name,
            WpfAnalyzersProvider.Name,
            XunitProvider.Name,
        };

        return list.ToArray();
    }

    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "OK.")]
    public async Task<Collection<AnalyzerProviderBaseRuleData>> CollectAllBaseRules(
        ProviderCollectingMode providerCollectingMode,
        bool logWithAnsiConsoleMarkup)
    {
        var data = new Collection<AnalyzerProviderBaseRuleData>();

        var asyncFixerProvider = new AsyncFixerProvider(logger, logWithAnsiConsoleMarkup);
        var asyncFixerTask = asyncFixerProvider.CollectBaseRules(providerCollectingMode);

        var asyncifyProvider = new AsyncifyProvider(logger, logWithAnsiConsoleMarkup);
        var asyncifyTask = asyncifyProvider.CollectBaseRules(providerCollectingMode);

        var meziantouProvider = new MeziantouProvider(logger, logWithAnsiConsoleMarkup);
        var meziantouTask = meziantouProvider.CollectBaseRules(providerCollectingMode);

        var microsoftCodeAnalysisNetAnalyzersProvider = new MicrosoftCodeAnalysisNetAnalyzersProvider(logger, logWithAnsiConsoleMarkup);
        var microsoftCodeAnalysisNetAnalyzersTask = microsoftCodeAnalysisNetAnalyzersProvider.CollectBaseRules(providerCollectingMode);

        var microsoftCompilerErrorsProvider = new MicrosoftCompilerErrorsProvider(logger, logWithAnsiConsoleMarkup);
        var microsoftCompilerErrorsTask = microsoftCompilerErrorsProvider.CollectBaseRules(providerCollectingMode);

        var microsoftCodeAnalysisNetAnalyzersProviderUndocumented = new MicrosoftCompilerErrorsProviderUndocumented(logger, logWithAnsiConsoleMarkup);
        var microsoftCompilerErrorsUndocumentedTask = microsoftCodeAnalysisNetAnalyzersProviderUndocumented.CollectBaseRules(ProviderCollectingMode.ReCollect);

        var microsoftVisualStudioThreadingAnalyzersProvider = new MicrosoftVisualStudioThreadingAnalyzersProvider(logger, logWithAnsiConsoleMarkup);
        var microsoftVisualStudioThreadingAnalyzersProviderTask = microsoftVisualStudioThreadingAnalyzersProvider.CollectBaseRules(ProviderCollectingMode.ReCollect);

        var nSubstituteAnalyzersProvider = new NSubstituteAnalyzersProvider(logger, logWithAnsiConsoleMarkup);
        var nSubstituteAnalyzersProviderTask = nSubstituteAnalyzersProvider.CollectBaseRules(ProviderCollectingMode.ReCollect);

        var securityCodeScanVs2019Provider = new SecurityCodeScanVs2019Provider(logger, logWithAnsiConsoleMarkup);
        var securityCodeScanVs2019Task = securityCodeScanVs2019Provider.CollectBaseRules(providerCollectingMode);

        var styleCopAnalyzersProvider = new StyleCopAnalyzersProvider(logger, logWithAnsiConsoleMarkup);
        var styleCopAnalyzersTask = styleCopAnalyzersProvider.CollectBaseRules(providerCollectingMode);

        var sonarAnalyzerCSharpProvider = new SonarAnalyzerCSharpProvider(logger, logWithAnsiConsoleMarkup);
        var sonarAnalyzerCSharpTask = sonarAnalyzerCSharpProvider.CollectBaseRules(providerCollectingMode);

        var wpfAnalyzersProvider = new WpfAnalyzersProvider(logger, logWithAnsiConsoleMarkup);
        var wpfAnalyzersProviderTask = wpfAnalyzersProvider.CollectBaseRules(providerCollectingMode);

        var xunitProvider = new XunitProvider(logger, logWithAnsiConsoleMarkup);
        var xunitProviderTask = xunitProvider.CollectBaseRules(providerCollectingMode);

        await Task.WhenAll(
            asyncFixerTask,
            asyncifyTask,
            meziantouTask,
            microsoftCodeAnalysisNetAnalyzersTask,
            microsoftCompilerErrorsTask,
            microsoftCompilerErrorsUndocumentedTask,
            microsoftVisualStudioThreadingAnalyzersProviderTask,
            nSubstituteAnalyzersProviderTask,
            securityCodeScanVs2019Task,
            styleCopAnalyzersTask,
            sonarAnalyzerCSharpTask,
            wpfAnalyzersProviderTask,
            xunitProviderTask);

        data.Add(await asyncFixerTask);
        data.Add(await asyncifyTask);
        data.Add(await meziantouTask);
        data.Add(await microsoftCodeAnalysisNetAnalyzersTask);
        data.Add(await microsoftCompilerErrorsTask);
        data.Add(await microsoftCompilerErrorsUndocumentedTask);
        data.Add(await microsoftVisualStudioThreadingAnalyzersProviderTask);
        data.Add(await nSubstituteAnalyzersProviderTask);
        data.Add(await securityCodeScanVs2019Task);
        data.Add(await styleCopAnalyzersTask);
        data.Add(await sonarAnalyzerCSharpTask);
        data.Add(await wpfAnalyzersProviderTask);
        data.Add(await xunitProviderTask);

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

        var microsoftVisualStudioThreadingAnalyzersProvider = new MicrosoftVisualStudioThreadingAnalyzersProvider(logger);
        microsoftVisualStudioThreadingAnalyzersProvider.Cleanup();

        var nSubstituteAnalyzersProvider = new NSubstituteAnalyzersProvider(logger);
        nSubstituteAnalyzersProvider.Cleanup();

        var securityCodeScanVs2019Provider = new SecurityCodeScanVs2019Provider(logger);
        securityCodeScanVs2019Provider.Cleanup();

        var styleCopAnalyzersProvider = new StyleCopAnalyzersProvider(logger);
        styleCopAnalyzersProvider.Cleanup();

        var sonarAnalyzerCSharpProvider = new SonarAnalyzerCSharpProvider(logger);
        sonarAnalyzerCSharpProvider.Cleanup();

        var wpfAnalyzersProvider = new WpfAnalyzersProvider(logger);
        wpfAnalyzersProvider.Cleanup();

        var xunitProvider = new XunitProvider(logger);
        xunitProvider.Cleanup();
    }
}