using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Atc.CodingRules.AnalyzerProviders.Models;

namespace Atc.CodingRules.AnalyzerProviders.Providers
{
    public class AnalyzerProviders
    {
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "OK.")]
        public Collection<AnalyzerProviderBaseRuleData> CollectAllBaseRules()
        {
            var data = new Collection<AnalyzerProviderBaseRuleData>();

            var asyncFixerProvider = new AsyncFixerProvider();
            var asyncFixerData = asyncFixerProvider.CollectBaseRules();
            data.Add(asyncFixerData);

            // TODO: Asyncify

            var meziantouProvider = new MeziantouProvider();
            var meziantouData = meziantouProvider.CollectBaseRules();
            data.Add(meziantouData);

            // TODO: Microsoft.CodeAnalysis.NetAnalyzers

            // TODO: SecurityCodeScan.VS2019

            // TODO: StyleCop.Analyzers

            // TODO: SonarAnalyzer.CSharp

            return data;
        }
    }
}