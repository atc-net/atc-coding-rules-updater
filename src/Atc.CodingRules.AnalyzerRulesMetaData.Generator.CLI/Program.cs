using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Atc.CodingRules.AnalyzerProviders;

namespace Atc.CodingRules.AnalyzerRulesMetaData.Generator.CLI
{
    public static class Program
    {
        private const string OutputFile = @"C:\Code\atc-net\atc-coding-rules-updater\AnalyzerRulesMetaData.json";

        public static async Task Main(string[] args)
        {
            var analyzerProviders = new AnalyzerProviderCollector();
            var apsData = await analyzerProviders.CollectAllBaseRules();

            var jsonOptions = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            };

            var json = JsonSerializer.Serialize(apsData, jsonOptions);
            await File.WriteAllTextAsync(OutputFile, json, Encoding.UTF8);
        }
    }
}