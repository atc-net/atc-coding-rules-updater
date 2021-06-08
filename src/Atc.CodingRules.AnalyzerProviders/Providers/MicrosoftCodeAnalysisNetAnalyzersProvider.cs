using System.Threading.Tasks;
using Atc.CodingRules.AnalyzerProviders.Models;

namespace Atc.CodingRules.AnalyzerProviders.Providers
{
    public class MicrosoftCodeAnalysisNetAnalyzersProvider : AnalyzerProviderBase
    {
        public override async Task<AnalyzerProviderBaseRuleData> CollectBaseRules()
        {
            var data = new AnalyzerProviderBaseRuleData("Microsoft.CodeAnalysis.NetAnalyzers");

            // TODO: Fix this..
            return data;
        }
    }
}