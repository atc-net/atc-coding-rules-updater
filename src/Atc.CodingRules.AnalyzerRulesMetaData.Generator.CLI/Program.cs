using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atc.CodingRules.AnalyzerRulesMetaData.Generator.CLI
{
    public static class Program
    {
        private const string OutputFile = @"C:\Code\atc-net\atc-coding-rules-updater\AnalyzerRulesMetaData.json";

        public static void Main(string[] args)
        {
            var analyzerProviders = new AnalyzerProviders.Providers.AnalyzerProviders();
            var apsData = analyzerProviders.CollectAllBaseRules();

            var jsonOptions = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            };

            var json = JsonSerializer.Serialize(apsData, jsonOptions);
            File.WriteAllText(OutputFile, json, Encoding.UTF8);
        }
    }
}