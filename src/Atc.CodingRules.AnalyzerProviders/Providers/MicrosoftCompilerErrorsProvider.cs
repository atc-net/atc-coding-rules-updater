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

                    var htmlDocChild = await web.LoadFromWebAsync(link).ConfigureAwait(false);
                    var headers1 = htmlDocChild.DocumentNode.SelectNodes("//h1").ToList();
                    var title = headers1.Count == 0
                        ? string.Empty
                        : headers1[0].InnerText;

                    var paragraphs = htmlDocChild.DocumentNode.SelectNodes("//p").ToList();
                    var description = headers1.Count == 0
                        ? string.Empty
                        : paragraphs[0].InnerText;

                    data.Rules.Add(
                        new Rule(
                            code,
                            title,
                            link,
                            category: null,
                            description));
                }
            }

            return data;
        }
    }
}