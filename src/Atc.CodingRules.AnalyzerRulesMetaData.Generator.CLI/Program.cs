using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Atc.CodingRules.AnalyzerRulesMetaData.Generator.CLI.AnalyzerProviders;
using Atc.CodingRules.AnalyzerRulesMetaData.Generator.CLI.Models;

namespace Atc.CodingRules.AnalyzerRulesMetaData.Generator.CLI
{
    class Program
    {
        static void Main(string[] args)
        {
            var aps = new List<AnalyzerProviderData>();

            var apAsyncFixerData = new AsyncFixerProvider().RetrieveData();
            aps.Add(apAsyncFixerData);

            var apMeziantouData = new MeziantouProvider().RetrieveData();
            aps.Add(apMeziantouData);

            string file = @"C:\Code\atc-net\atc-coding-rules-updater\AnalyzerRulesMetaData.json";
            var json = JsonSerializer.Serialize(aps, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(file, json);
        }
    }
}