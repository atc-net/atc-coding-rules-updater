using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Atc.CodingRules.AnalyzerProviders.Models;
using HtmlAgilityPack;

namespace Atc.CodingRules.AnalyzerProviders.Providers
{
    public class MicrosoftCompilerErrorsProvider : AnalyzerProviderBase
    {
        public override Uri? DocumentationLink { get; set; } = new Uri("https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages", UriKind.Absolute);

        public override async Task<AnalyzerProviderBaseRuleData> CollectBaseRules()
        {
            var data = new AnalyzerProviderBaseRuleData("Microsoft.CompilerErrors");

            var web = new HtmlWeb();
            var htmlDoc = await web.LoadFromWebAsync(DocumentationLink!.AbsoluteUri + "/toc.json").ConfigureAwait(false);
            var titleValue = htmlDoc.DocumentNode.SelectSingleNode("//*//title")?.InnerText;
            if (titleValue is not null
                && (
                    titleValue.Contains("access", StringComparison.OrdinalIgnoreCase)
                    || titleValue.Contains("denied", StringComparison.OrdinalIgnoreCase)
                   )
                )
            {
                data.ExceptionMessage = "Access Denied";
                return data;
            }

            var jsonDoc = JsonDocument.Parse(htmlDoc.DocumentNode.InnerText);
            var jsonDocItems = jsonDoc.RootElement.GetProperty("items").EnumerateArray();

            var ruleTasks = new List<Task<Rule?>>();
            while (jsonDocItems.MoveNext())
            {
                var jsonElement = jsonDocItems.Current;
                if (!jsonElement.GetRawText().Contains("children", StringComparison.Ordinal))
                {
                    continue;
                }

                var jsonChildItems = jsonElement.GetProperty("children").EnumerateArray();
                while (jsonChildItems.MoveNext())
                {
                    var jsonChildElement = jsonChildItems.Current;
                    var code = jsonChildElement.GetProperty("toc_title").ToString()!;
                    var hrefPart = jsonChildElement.GetProperty("href").ToString()!;

                    var link = hrefPart.StartsWith("../../misc/", StringComparison.Ordinal)
                        ? "https://docs.microsoft.com/en-us/dotnet/csharp/" + hrefPart.Replace("../../", string.Empty, StringComparison.Ordinal)
                        : DocumentationLink.AbsoluteUri + "/" + hrefPart;

                    var ruleTask = GetRuleByCode(code, link);
                    ruleTasks.Add(ruleTask);
                }
            }

            await Task.WhenAll(ruleTasks.ToArray());
            foreach (var ruleTask in ruleTasks)
            {
                var rule = await ruleTask;
                if (rule is not null)
                {
                    data.Rules.Add(rule);
                }
            }

            return data;
        }

        [SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "OK.")]
        [SuppressMessage("Security", "SCS0005:Weak random generator", Justification = "OK.")]
        private static async Task<Rule?> GetRuleByCode(string code, string link)
        {
            // Try not to look like DDoS-attack
            var rnd = new Random();
            int nextMs = rnd.Next(500, 1000);
            await Task.Delay(nextMs);

            var web = new HtmlWeb();
            var htmlDoc = await web.LoadFromWebAsync(link).ConfigureAwait(false);
            var titleValue = htmlDoc.DocumentNode.SelectSingleNode("//*//title")?.InnerText;
            if (titleValue is not null
                && (
                    titleValue.Contains("access", StringComparison.OrdinalIgnoreCase)
                    || titleValue.Contains("denied", StringComparison.OrdinalIgnoreCase)
                   )
                )
            {
                return null;
            }

            var headers1 = htmlDoc.DocumentNode.SelectNodes("//h1").ToList();
            var title = headers1.Count == 0
                ? string.Empty
                : headers1[0].InnerText;

            var paragraphs = htmlDoc.DocumentNode.SelectNodes("//p").ToList();
            var description = headers1.Count == 0
                ? string.Empty
                : paragraphs[0].InnerText;

            return new Rule(
                    code,
                    title,
                    link,
                    category: null,
                    description);
        }
    }
}