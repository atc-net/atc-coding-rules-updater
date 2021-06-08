using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Atc.CodingRules.AnalyzerProviders.Models;
using Atc.CodingRules.AnalyzerProviders.Providers;

namespace Atc.CodingRules.AnalyzerProviders
{
    public class AnalyzerProviderCollector
    {
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "OK.")]
        public async Task<Collection<AnalyzerProviderBaseRuleData>> CollectAllBaseRules()
        {
            var data = new Collection<AnalyzerProviderBaseRuleData>();

            var asyncFixerProvider = new AsyncFixerProvider();
            var asyncFixerData = await asyncFixerProvider.CollectBaseRules();
            data.Add(asyncFixerData);

            var asyncifyProvider = new AsyncifyProvider();
            var asyncifyData = await asyncifyProvider.CollectBaseRules();
            data.Add(asyncifyData);

            var meziantouProvider = new MeziantouProvider();
            var meziantouData = await meziantouProvider.CollectBaseRules();
            data.Add(meziantouData);

            var microsoftCodeAnalysisNetAnalyzersProvider = new MicrosoftCodeAnalysisNetAnalyzersProvider();
            var microsoftCodeAnalysisNetAnalyzersData = await microsoftCodeAnalysisNetAnalyzersProvider.CollectBaseRules();
            data.Add(microsoftCodeAnalysisNetAnalyzersData);

            var securityCodeScanVs2019Provider = new SecurityCodeScanVs2019Provider();
            var securityCodeScanVs2019Data = await securityCodeScanVs2019Provider.CollectBaseRules();
            data.Add(securityCodeScanVs2019Data);

            var styleCopAnalyzersProvider = new StyleCopAnalyzersProvider();
            var styleCopAnalyzersData = await styleCopAnalyzersProvider.CollectBaseRules();
            data.Add(styleCopAnalyzersData);

            var sonarAnalyzerCSharpProvider = new SonarAnalyzerCSharpProvider();
            var sonarAnalyzerCSharpData = await sonarAnalyzerCSharpProvider.CollectBaseRules();
            data.Add(sonarAnalyzerCSharpData);

            return data;
        }
    }
}