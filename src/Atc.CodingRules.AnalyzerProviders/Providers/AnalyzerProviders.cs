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

            var asyncifyProvider = new AsyncifyProvider();
            var asyncifyData = asyncifyProvider.CollectBaseRules();
            data.Add(asyncifyData);

            var meziantouProvider = new MeziantouProvider();
            var meziantouData = meziantouProvider.CollectBaseRules();
            data.Add(meziantouData);

            var microsoftCodeAnalysisNetAnalyzersProvider = new MicrosoftCodeAnalysisNetAnalyzersProvider();
            var microsoftCodeAnalysisNetAnalyzersData = microsoftCodeAnalysisNetAnalyzersProvider.CollectBaseRules();
            data.Add(microsoftCodeAnalysisNetAnalyzersData);

            var securityCodeScanVs2019Provider = new SecurityCodeScanVs2019Provider();
            var securityCodeScanVs2019Data = securityCodeScanVs2019Provider.CollectBaseRules();
            data.Add(securityCodeScanVs2019Data);

            var styleCopAnalyzersProvider = new StyleCopAnalyzersProvider();
            var styleCopAnalyzersData = styleCopAnalyzersProvider.CollectBaseRules();
            data.Add(styleCopAnalyzersData);

            var sonarAnalyzerCSharpProvider = new SonarAnalyzerCSharpProvider();
            var sonarAnalyzerCSharpData = sonarAnalyzerCSharpProvider.CollectBaseRules();
            data.Add(sonarAnalyzerCSharpData);

            return data;
        }
    }
}