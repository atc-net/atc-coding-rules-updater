using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
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
        private const string GitRawAtcAnalyzerProviderBaseRulesFileName = "https://raw.githubusercontent.com/atc-net/atc-coding-rules-updater/main/" + AtcAnalyzerProviderBaseRulesFileName;
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public static async Task<Collection<AnalyzerProviderBaseRuleData>> GetAnalyzerProviderBaseRules(CancellationToken cancellationToken = default)
        {
            var tempFile = Path.Combine(Path.GetTempPath(), AtcAnalyzerProviderBaseRulesFileName);
            var fileInfo = new FileInfo(tempFile);
            if (fileInfo.Exists && fileInfo.LastWriteTimeUtc > DateTime.UtcNow.AddMonths(-1))
            {
                var fileAsJson = await File.ReadAllTextAsync(tempFile, cancellationToken);
                return JsonSerializer.Deserialize<Collection<AnalyzerProviderBaseRuleData>>(fileAsJson, JsonOptions);
            }

            Collection<AnalyzerProviderBaseRuleData>? analyzerProviderBaseRules = null;
            Colorful.Console.WriteLine("Working on collecting rules metadata.", Color.Tan);
            Colorful.Console.WriteLine($"- start {DateTime.Now:T}", Color.Tan);

            analyzerProviderBaseRules = TryGetAnalyzerProviderBaseRulesFromGitHub();
            if (analyzerProviderBaseRules is null)
            {
                var analyzerProviders = new AnalyzerProviderCollector();
                analyzerProviderBaseRules = await analyzerProviders.CollectAllBaseRules(cancellationToken);
            }

            Colorful.Console.WriteLine($"- end {DateTime.Now:T}", Color.Tan);
            Console.WriteLine();

            var hasErrors = analyzerProviderBaseRules.Any(x => !string.IsNullOrEmpty(x.ExceptionMessage));
            if (!hasErrors)
            {
                var json = JsonSerializer.Serialize(analyzerProviderBaseRules, JsonOptions);
                await File.WriteAllTextAsync(tempFile, json, Encoding.UTF8, cancellationToken);
            }

            return analyzerProviderBaseRules;
        }

        [SuppressMessage("Major Code Smell", "S1168:Empty arrays and collections should be returned instead of null", Justification = "OK. - By design.")]
        private static Collection<AnalyzerProviderBaseRuleData>? TryGetAnalyzerProviderBaseRulesFromGitHub()
        {
            var rawGitData = HttpClientHelper.GetRawFile(GitRawAtcAnalyzerProviderBaseRulesFileName);
            return string.IsNullOrEmpty(rawGitData)
                ? null
                : JsonSerializer.Deserialize<Collection<AnalyzerProviderBaseRuleData>>(rawGitData, JsonOptions);
        }
    }
}