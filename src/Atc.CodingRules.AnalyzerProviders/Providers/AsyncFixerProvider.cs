using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Atc.CodingRules.AnalyzerProviders.Models;
using HtmlAgilityPack;

namespace Atc.CodingRules.AnalyzerProviders.Providers
{
    public class AsyncFixerProvider : AnalyzerProviderBase
    {
        [SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification = "OK.")]
        public override Uri? DocumentationLink { get; set; } = new Uri("https://github.com/semihokur/AsyncFixer/blob/main/README.md", UriKind.Absolute);

        public override async Task<AnalyzerProviderBaseRuleData> CollectBaseRules()
        {
            var data = new AnalyzerProviderBaseRuleData("AsyncFixer");

            var web = new HtmlWeb();
            var htmlDoc = await web.LoadFromWebAsync(DocumentationLink!.AbsoluteUri);

            var headers3 = htmlDoc.DocumentNode.SelectNodes("//h3").ToList();

            foreach (var item in headers3)
            {
                if (!item.InnerText.StartsWith("Async", StringComparison.Ordinal))
                {
                    continue;
                }

                var description = item.NextSibling.NextSibling.InnerText.Replace(" Here is an example:", string.Empty, StringComparison.OrdinalIgnoreCase);

                var sa = item.InnerText.Split(':');
                var code = sa[0];
                var title = sa[1];
                var link = $"{DocumentationLink}#{code.ToLower(GlobalizationConstants.EnglishCultureInfo)}{title.ToLower(GlobalizationConstants.EnglishCultureInfo).Replace(" ", "-", StringComparison.Ordinal)}";

                data.Rules.Add(
                    new Rule(
                        code,
                        title,
                        link,
                        description: description));
            }

            return data;
        }
    }
}