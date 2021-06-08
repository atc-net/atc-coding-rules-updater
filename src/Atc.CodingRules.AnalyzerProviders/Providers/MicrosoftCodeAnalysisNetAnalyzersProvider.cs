using Atc.CodingRules.AnalyzerProviders.Models;

namespace Atc.CodingRules.AnalyzerProviders.Providers
{
    public class MicrosoftCodeAnalysisNetAnalyzersProvider : AnalyzerProviderBase
    {
        public override AnalyzerProviderBaseRuleData CollectBaseRules()
        {
            var data = new AnalyzerProviderBaseRuleData("Microsoft.CodeAnalysis.NetAnalyzers");

            // TODO: Fix this..
            return data;
        }
    }
}