using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Atc.CodingRules.AnalyzerProviders.Models;
using Atc.CodingRules.AnalyzerProviders.Providers;

namespace Atc.CodingRules.AnalyzerProviders
{
    public class AnalyzerProviderCollector
    {
        public static string[] GetAllBaseRuleProviderNames()
        {
            var list = new List<string>
            {
                AsyncFixerProvider.Name,
                AsyncifyProvider.Name,
                MeziantouProvider.Name,
                MicrosoftCodeAnalysisNetAnalyzersProvider.Name,
                MicrosoftCompilerErrorsProvider.Name,
                MicrosoftCompilerErrorsProviderUndocumented.Name,
                SecurityCodeScanVs2019Provider.Name,
                StyleCopAnalyzersProvider.Name,
                SonarAnalyzerCSharpProvider.Name,
            };

            return list.ToArray();
        }

        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "OK.")]
        public async Task<Collection<AnalyzerProviderBaseRuleData>> CollectAllBaseRules(ProviderCollectingMode providerCollectingMode, CancellationToken cancellationToken = default)
        {
            var data = new Collection<AnalyzerProviderBaseRuleData>();

            var asyncFixerProvider = new AsyncFixerProvider();
            var asyncFixerTask = asyncFixerProvider.CollectBaseRules(providerCollectingMode);

            var asyncifyProvider = new AsyncifyProvider();
            var asyncifyTask = asyncifyProvider.CollectBaseRules(providerCollectingMode);

            var meziantouProvider = new MeziantouProvider();
            var meziantouTask = meziantouProvider.CollectBaseRules(providerCollectingMode);

            var microsoftCodeAnalysisNetAnalyzersProvider = new MicrosoftCodeAnalysisNetAnalyzersProvider();
            var microsoftCodeAnalysisNetAnalyzersTask = microsoftCodeAnalysisNetAnalyzersProvider.CollectBaseRules(providerCollectingMode);

            var microsoftCompilerErrorsProvider = new MicrosoftCompilerErrorsProvider();
            var microsoftCompilerErrorsTask = microsoftCompilerErrorsProvider.CollectBaseRules(providerCollectingMode);

            var microsoftCodeAnalysisNetAnalyzersProviderUndocumented = new MicrosoftCompilerErrorsProviderUndocumented();
            var microsoftCompilerErrorsUndocumentedTask = microsoftCodeAnalysisNetAnalyzersProviderUndocumented.CollectBaseRules(ProviderCollectingMode.ReCollect);

            var securityCodeScanVs2019Provider = new SecurityCodeScanVs2019Provider();
            var securityCodeScanVs2019Task = securityCodeScanVs2019Provider.CollectBaseRules(providerCollectingMode);

            var styleCopAnalyzersProvider = new StyleCopAnalyzersProvider();
            var styleCopAnalyzersTask = styleCopAnalyzersProvider.CollectBaseRules(providerCollectingMode);

            var sonarAnalyzerCSharpProvider = new SonarAnalyzerCSharpProvider();
            var sonarAnalyzerCSharpTask = sonarAnalyzerCSharpProvider.CollectBaseRules(providerCollectingMode);

            await Task.WhenAll(
                asyncFixerTask,
                asyncifyTask,
                meziantouTask,
                microsoftCodeAnalysisNetAnalyzersTask,
                microsoftCompilerErrorsTask,
                microsoftCompilerErrorsUndocumentedTask,
                securityCodeScanVs2019Task,
                styleCopAnalyzersTask,
                sonarAnalyzerCSharpTask);

            data.Add(await asyncFixerTask);
            data.Add(await asyncifyTask);
            data.Add(await meziantouTask);
            data.Add(await microsoftCodeAnalysisNetAnalyzersTask);
            data.Add(await microsoftCompilerErrorsTask);
            data.Add(await microsoftCompilerErrorsUndocumentedTask);
            data.Add(await securityCodeScanVs2019Task);
            data.Add(await styleCopAnalyzersTask);
            data.Add(await sonarAnalyzerCSharpTask);

            return data;
        }
    }
}