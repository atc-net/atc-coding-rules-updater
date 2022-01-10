namespace Atc.CodingRules.AnalyzerProviders;

public static class AnalyzerProviderBaseRulesHelper
{
    public static async Task<Collection<AnalyzerProviderBaseRuleData>> GetAnalyzerProviderBaseRules(
        ILogger logger,
        ProviderCollectingMode providerCollectingMode)
    {
        var stopwatch = Stopwatch.StartNew();
        logger.LogTrace("     Collecting rules metadata");

        var analyzerProviders = new AnalyzerProviderCollector(logger);
        var analyzerProviderBaseRules = await analyzerProviders.CollectAllBaseRules(providerCollectingMode);

        stopwatch.Stop();
        logger.LogTrace($"     Collecting rules metadata time: {stopwatch.Elapsed.GetPrettyTime()}");

        LogAnalyzerProviderInformation(logger, analyzerProviderBaseRules);
        LogAnalyzerProviderErrors(logger, analyzerProviderBaseRules);

        return analyzerProviderBaseRules;
    }

    public static void CleanupCache(
        ILogger logger)
    {
        var analyzerProviders = new AnalyzerProviderCollector(logger);
        analyzerProviders.CacheCleanup();
    }

    private static void LogAnalyzerProviderInformation(
        ILogger logger,
        IReadOnlyCollection<AnalyzerProviderBaseRuleData> analyzerProviderBaseRules)
    {
        var rulesCount = analyzerProviderBaseRules.Sum(x => x.Rules.Count);
        logger.LogTrace($"     Loaded {analyzerProviderBaseRules.Count} providers with {rulesCount} rules");
    }

    private static void LogAnalyzerProviderErrors(
        ILogger logger,
        IEnumerable<AnalyzerProviderBaseRuleData> analyzerProviderBaseRules)
    {
        foreach (var item in analyzerProviderBaseRules)
        {
            if (item.ExceptionMessage is not null)
            {
                logger.LogError($"     AnalyzerProvider-{item.Name} - {item.ExceptionMessage}");
            }
        }
    }
}