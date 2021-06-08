using Atc.CodingRules.AnalyzerProviders.Models;

namespace Atc.CodingRules.AnalyzerProviders.Providers
{
    public interface IAnalyzerProvider
    {
        AnalyzerProviderBaseRuleData CollectBaseRules();
    }
}