using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atc.CodingRules.AnalyzerProviders.Models;
using HtmlAgilityPack;

// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
namespace Atc.CodingRules.AnalyzerProviders.Providers
{
    public class StyleCopAnalyzersProvider : AnalyzerProviderBase
    {
        private const int TableColumnId = 0;
        private const int TableColumnTitle = 1;
        private const int TableColumnDescription = 2;

        public static string Name => "StyleCop.Analyzers";

        public override Uri? DocumentationLink { get; set; } = new Uri("https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/DOCUMENTATION.md", UriKind.Absolute);

        protected override AnalyzerProviderBaseRuleData CreateData()
        {
            return new AnalyzerProviderBaseRuleData(Name);
        }

        protected override async Task ReCollect(AnalyzerProviderBaseRuleData data)
        {
            var web = new HtmlWeb();
            var htmlDoc = await web.LoadFromWebAsync(DocumentationLink!.AbsoluteUri).ConfigureAwait(false);
            var articleNode = htmlDoc.DocumentNode.SelectNodes("//article[@class='markdown-body entry-content container-lg']").First();
            var articleRuleLinks = articleNode.SelectNodes("//*//strong//a").ToList();

            foreach (var item in articleRuleLinks.Where(x => x.Attributes.Count == 1 && x.InnerText.Contains("(S", StringComparison.Ordinal)))
            {
                var rules = await GetRules(item);
                foreach (var rule in rules)
                {
                    data.Rules.Add(rule);
                }
            }
        }

        private static async Task<List<Rule>> GetRules(HtmlNode item)
        {
            var link = $"https://github.com{item.Attributes["href"].Value}";
            var web = new HtmlWeb();
            var htmlDoc = await web.LoadFromWebAsync(link).ConfigureAwait(false);
            var articleNode = htmlDoc.DocumentNode.SelectNodes("//article[@class='markdown-body entry-content container-lg']").First();
            var articleTableRows = articleNode.SelectNodes("//*//table[1]//tr").ToList();
            var category = articleNode.Descendants("h3").First().InnerText;
            var i = category.IndexOf(" Rules", StringComparison.Ordinal);
            if (i > 0)
            {
                category = category.Substring(0, i);
            }

            var rules = new List<Rule>();
            foreach (var row in articleTableRows)
            {
                if (row.SelectNodes("td") is null)
                {
                    continue;
                }

                var cells = row.SelectNodes("td").ToList();
                if (cells.Count <= 0)
                {
                    continue;
                }

                var aHrefNode = cells[TableColumnId].SelectSingleNode("a");
                if (aHrefNode is null)
                {
                    continue;
                }

                var code = aHrefNode.InnerText;
                var title = HtmlEntity.DeEntitize(cells[TableColumnTitle].InnerText).NormalizePascalCase();
                var helpLink = $"https://github.com{aHrefNode.Attributes["href"].Value}";
                var description = cells[TableColumnDescription].InnerText;

                rules.Add(
                    new Rule(
                        code,
                        title,
                        helpLink,
                        category,
                        description));
            }

            return rules;
        }
    }
}