using Atc.CodingRules.AnalyzerProviders.Models;

namespace Atc.CodingRules.AnalyzerProviders.Providers
{
    public class SecurityCodeScanVs2019Provider : AnalyzerProviderBase
    {
        public override AnalyzerProviderBaseRuleData CollectBaseRules()
        {
            var data = new AnalyzerProviderBaseRuleData("SecurityCodeScan.VS2019");

            // TODO: Fix this..
            return data;
        }
    }
}