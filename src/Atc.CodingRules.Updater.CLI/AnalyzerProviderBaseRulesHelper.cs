// ReSharper disable InvertIf
namespace Atc.CodingRules.Updater.CLI;

public static class AnalyzerProviderBaseRulesHelper
{
    public static async Task<Collection<AnalyzerProviderBaseRuleData>> GetAnalyzerProviderBaseRules(
        ILogger logger,
        ProviderCollectingMode providerCollectingMode,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation($"Working on collecting rules metadata - start {DateTime.Now:T}.");

        var analyzerProviders = new AnalyzerProviderCollector();
        var analyzerProviderBaseRules = await analyzerProviders.CollectAllBaseRules(providerCollectingMode, cancellationToken);

        logger.LogInformation($"Rules metadata collected - end {DateTime.Now:T}.");
        return analyzerProviderBaseRules;
    }
}