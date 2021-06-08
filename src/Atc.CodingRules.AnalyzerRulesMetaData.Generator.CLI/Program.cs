using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Atc.CodingRules.AnalyzerRulesMetaData.Generator.CLI
{
    class Program
    {
        static void Main(string[] args)
        {
            var analyzerProviders = new AnalyzerProviders.Providers.AnalyzerProviders();
            var apsData = analyzerProviders.CollectAllBaseRules();

            var jsonOptions = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true,
            };

            string file = @"C:\Code\atc-net\atc-coding-rules-updater\AnalyzerRulesMetaData.json";

            var json = JsonSerializer.Serialize(apsData, jsonOptions);
            File.WriteAllText(file, json, Encoding.UTF8);
        }
    }
}