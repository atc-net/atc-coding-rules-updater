using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Atc.CodingRules.AnalyzerProviders;
using Atc.CodingRules.AnalyzerProviders.Models;

namespace Atc.CodingRules.Updater.CLI
{
    public static class AnalyzerProviderBaseRulesHelper
    {
        public static async Task<Collection<AnalyzerProviderBaseRuleData>> GetAnalyzerProviderBaseRules(CancellationToken cancellationToken = default)
        {
            var jsonOptions = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            };

            var tempFile = Path.Combine(Path.GetTempPath(), "AtcAnalyzerProviderBaseRules.json");
            if (File.Exists(tempFile) /* TODO: && check not older the 1day */)
            {
                var fileAsJson = await File.ReadAllTextAsync(tempFile, cancellationToken);
                return JsonSerializer.Deserialize<Collection<AnalyzerProviderBaseRuleData>>(fileAsJson, jsonOptions);
            }

            var analyzerProviders = new AnalyzerProviderCollector();
            var analyzerProviderBaseRules = await analyzerProviders.CollectAllBaseRules(cancellationToken);

            var json = JsonSerializer.Serialize(analyzerProviderBaseRules, jsonOptions);
            await File.WriteAllTextAsync(tempFile, json, Encoding.UTF8, cancellationToken);

            return analyzerProviderBaseRules;
        }
    }
}