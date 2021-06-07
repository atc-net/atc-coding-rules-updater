using Atc.CodingRules.AnalyzerRulesMetaData.Generator.CLI.Models;

namespace Atc.CodingRules.AnalyzerRulesMetaData.Generator.CLI.AnalyzerProviders
{
    public class AsyncFixerProvider : AnalyzerProviderBase
    {
        public override AnalyzerProviderData RetrieveData()
        {
            var ap = new AnalyzerProviderData("AsyncFixer");

            // TODO: Fix this..

            return ap;
        }
    }
}