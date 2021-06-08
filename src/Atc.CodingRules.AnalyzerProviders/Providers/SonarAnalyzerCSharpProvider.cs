using Atc.CodingRules.AnalyzerProviders.Models;

namespace Atc.CodingRules.AnalyzerProviders.Providers
{
    public class SonarAnalyzerCSharpProvider : AnalyzerProviderBase
    {
        public override AnalyzerProviderBaseRuleData CollectBaseRules()
        {
            var data = new AnalyzerProviderBaseRuleData("SonarAnalyzer.CSharp");

            // TODO: Fix this..
            return data;
        }
    }
}