using Atc.CodingRules.AnalyzerProviders.Models;

namespace Atc.CodingRules.AnalyzerProviders.Providers
{
    public class StyleCopAnalyzersProvider : AnalyzerProviderBase
    {
        public override AnalyzerProviderBaseRuleData CollectBaseRules()
        {
            var data = new AnalyzerProviderBaseRuleData("StyleCop.Analyzers");

            // TODO: Fix this..

            return data;
        }
    }
}