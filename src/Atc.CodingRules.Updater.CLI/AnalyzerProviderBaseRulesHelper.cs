using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Atc.CodingRules.AnalyzerProviders;
using Atc.CodingRules.AnalyzerProviders.Models;

// ReSharper disable InvertIf
namespace Atc.CodingRules.Updater.CLI
{
    public static class AnalyzerProviderBaseRulesHelper
    {
        private const string AtcAnalyzerProviderBaseRulesFileName = "AtcAnalyzerProviderBaseRules.json";

        public static async Task<Collection<AnalyzerProviderBaseRuleData>> GetAnalyzerProviderBaseRules(CancellationToken cancellationToken = default)
        {
            var jsonOptions = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            };

            var tempFile = Path.Combine(Path.GetTempPath(), AtcAnalyzerProviderBaseRulesFileName);
            var fileInfo = new FileInfo(tempFile);
            if (fileInfo.Exists && fileInfo.LastWriteTimeUtc > DateTime.UtcNow.AddDays(-1))
            {
                var fileAsJson = await File.ReadAllTextAsync(tempFile, cancellationToken);
                return JsonSerializer.Deserialize<Collection<AnalyzerProviderBaseRuleData>>(fileAsJson, jsonOptions);
            }

            var analyzerProviders = new AnalyzerProviderCollector();
            var analyzerProviderBaseRules = await analyzerProviders.CollectAllBaseRules(cancellationToken);

            bool hasErrors = analyzerProviderBaseRules.Any(x => !string.IsNullOrEmpty(x.ExceptionMessage));
            if (!hasErrors)
            {
                var json = JsonSerializer.Serialize(analyzerProviderBaseRules, jsonOptions);
                await File.WriteAllTextAsync(tempFile, json, Encoding.UTF8, cancellationToken);
            }

            return analyzerProviderBaseRules;
        }
    }
}