using Atc.CodingRules.AnalyzerRulesMetaData.Generator.CLI.Models;

namespace Atc.CodingRules.AnalyzerRulesMetaData.Generator.CLI.AnalyzerProviders
{
    public interface IAnalyzerProvider
    {
        AnalyzerProviderData RetrieveData();
    }
}