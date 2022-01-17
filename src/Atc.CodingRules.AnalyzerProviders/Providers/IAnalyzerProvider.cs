namespace Atc.CodingRules.AnalyzerProviders.Providers;

public interface IAnalyzerProvider
{
    Task<AnalyzerProviderBaseRuleData> CollectBaseRules(
        ProviderCollectingMode providerCollectingMode);
}