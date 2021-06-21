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
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "OK.")]
        public async Task<Collection<AnalyzerProviderBaseRuleData>> CollectAllBaseRules(CancellationToken cancellationToken = default)
        {
            var data = new Collection<AnalyzerProviderBaseRuleData>();

            var asyncFixerProvider = new AsyncFixerProvider();
            var asyncFixerTask = asyncFixerProvider.CollectBaseRules();

            var asyncifyProvider = new AsyncifyProvider();
            var asyncifyTask = asyncifyProvider.CollectBaseRules();

            var meziantouProvider = new MeziantouProvider();
            var meziantouTask = meziantouProvider.CollectBaseRules();

            var microsoftCodeAnalysisNetAnalyzersProvider = new MicrosoftCodeAnalysisNetAnalyzersProvider();
            var microsoftCodeAnalysisNetAnalyzersTask = microsoftCodeAnalysisNetAnalyzersProvider.CollectBaseRules();

            var microsoftCompilerErrorsProvider = new MicrosoftCompilerErrorsProvider();
            var microsoftCompilerErrorsTask = microsoftCompilerErrorsProvider.CollectBaseRules();

            var securityCodeScanVs2019Provider = new SecurityCodeScanVs2019Provider();
            var securityCodeScanVs2019Task = securityCodeScanVs2019Provider.CollectBaseRules();

            var styleCopAnalyzersProvider = new StyleCopAnalyzersProvider();
            var styleCopAnalyzersTask = styleCopAnalyzersProvider.CollectBaseRules();

            var sonarAnalyzerCSharpProvider = new SonarAnalyzerCSharpProvider();
            var sonarAnalyzerCSharpTask = sonarAnalyzerCSharpProvider.CollectBaseRules();

            await Task.WhenAll(
                asyncFixerTask,
                asyncifyTask,
                meziantouTask,
                microsoftCodeAnalysisNetAnalyzersTask,
                microsoftCompilerErrorsTask,
                securityCodeScanVs2019Task,
                styleCopAnalyzersTask,
                sonarAnalyzerCSharpTask);

            data.Add(await asyncFixerTask);
            data.Add(await asyncifyTask);
            data.Add(await meziantouTask);
            data.Add(await microsoftCodeAnalysisNetAnalyzersTask);
            data.Add(await microsoftCompilerErrorsTask);
            data.Add(await securityCodeScanVs2019Task);
            data.Add(await styleCopAnalyzersTask);
            data.Add(await sonarAnalyzerCSharpTask);

            return data;
        }
    }
}