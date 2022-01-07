using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Atc.CodingRules.AnalyzerProviders;
using Atc.CodingRules.AnalyzerProviders.Models;
using Microsoft.Extensions.Logging;

// ReSharper disable InvertIf
namespace Atc.CodingRules.Updater.CLI
{
    public static class AnalyzerProviderBaseRulesHelper
    {
        public static async Task<Collection<AnalyzerProviderBaseRuleData>> GetAnalyzerProviderBaseRules(
            ILogger logger,
            ProviderCollectingMode providerCollectingMode,
            CancellationToken cancellationToken = default)
        {
            logger.LogInformation($"Working on collecting rules metadata - start {DateTime.Now:T}.");

            var analyzerProviders = new AnalyzerProviderCollector();
            var analyzerProviderBaseRules = await analyzerProviders.CollectAllBaseRules(providerCollectingMode, cancellationToken);

            logger.LogInformation($"Rules metadata collected - end {DateTime.Now:T}.");
            return analyzerProviderBaseRules;
        }
    }
}