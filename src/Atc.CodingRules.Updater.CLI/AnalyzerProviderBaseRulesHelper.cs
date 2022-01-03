using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Atc.CodingRules.AnalyzerProviders;
using Atc.CodingRules.AnalyzerProviders.Models;

// ReSharper disable InvertIf
namespace Atc.CodingRules.Updater.CLI
{
    public static class AnalyzerProviderBaseRulesHelper
    {
        public static async Task<Collection<AnalyzerProviderBaseRuleData>> GetAnalyzerProviderBaseRules(ProviderCollectingMode providerCollectingMode, CancellationToken cancellationToken = default)
        {
            Colorful.Console.WriteLine("Working on collecting rules metadata.", Color.Tan);
            Colorful.Console.WriteLine($"- start {DateTime.Now:T}", Color.Tan);

            var analyzerProviders = new AnalyzerProviderCollector();
            var analyzerProviderBaseRules = await analyzerProviders.CollectAllBaseRules(providerCollectingMode, cancellationToken);

            Colorful.Console.WriteLine($"- end {DateTime.Now:T}", Color.Tan);
            Console.WriteLine();

            return analyzerProviderBaseRules;
        }
    }
}