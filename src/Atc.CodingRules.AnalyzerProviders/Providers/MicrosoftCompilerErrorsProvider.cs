using System;
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
            if (htmlDoc.DocumentNode.HasTitleWithAccessDenied())
            {
                data.ExceptionMessage = "Access Denied";
                return data;
            }

            var jsonDoc = JsonDocument.Parse(htmlDoc.DocumentNode.InnerText);
            var jsonDocItems = jsonDoc.RootElement.GetProperty("items").EnumerateArray();

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

                    var rule = await GetRuleByCode(code, link);
                    if (rule is not null)
                    {
                        data.Rules.Add(rule);
                    }
                }
            }

            return data;
        }

        private static async Task<Rule?> GetRuleByCode(string code, string link)
        {
            var web = new HtmlWeb();
            var htmlDoc = await web.LoadFromWebAsync(link).ConfigureAwait(false);
            if (htmlDoc.DocumentNode.HasTitleWithAccessDenied())
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