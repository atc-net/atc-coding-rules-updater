using System;
using System.Linq;
using System.Threading.Tasks;
using Atc.CodingRules.AnalyzerProviders.Models;
using HtmlAgilityPack;

namespace Atc.CodingRules.AnalyzerProviders.Providers
{
    public class StyleCopAnalyzersProvider : AnalyzerProviderBase
    {
        private const int TableColumnId = 0;
        private const int TableColumnTitle = 1;
        private const int TableColumnDescription = 2;

        public override Uri? DocumentationLink { get; set; } = new Uri("https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/DOCUMENTATION.md", UriKind.Absolute);

        public override async Task<AnalyzerProviderBaseRuleData> CollectBaseRules()
        {
            var data = new AnalyzerProviderBaseRuleData("StyleCop.Analyzers");

            var web = new HtmlWeb();
            var htmlDoc = await web.LoadFromWebAsync(DocumentationLink!.AbsoluteUri).ConfigureAwait(false);
            var articleNode = htmlDoc.DocumentNode.SelectNodes("//article[@class='markdown-body entry-content container-lg']").First();
            var articleRuleLinks = articleNode.SelectNodes("//*//strong//a").ToList();

            foreach (var item in articleRuleLinks)
            {
                if (item.Attributes.Count != 1 ||
                    !item.InnerText.Contains("(S", StringComparison.Ordinal))
                {
                    continue;
                }

                var webChild = new HtmlWeb();
                var htmlDocChild = webChild.Load(new Uri($"https://github.com{item.Attributes[0].Value}", UriKind.Absolute));
                var articleNodeChild = htmlDocChild.DocumentNode.SelectNodes("//article[@class='markdown-body entry-content container-lg']").First();
                var articleTableRowsChild = articleNodeChild.SelectNodes("//*//table[1]//tr").ToList();
                var category = articleNodeChild.Descendants("h3").First().InnerText;
                var i = category.IndexOf(" Rules", StringComparison.Ordinal);
                if (i > 0)
                {
                    category = category.Substring(0, i);
                }

                foreach (var row in articleTableRowsChild)
                {
                    if (row.SelectNodes("td") == null)
                    {
                        continue;
                    }

                    var cells = row.SelectNodes("td").ToList();
                    if (cells.Count <= 0)
                    {
                        continue;
                    }

                    var aHrefNode = cells[TableColumnId].SelectSingleNode("a");
                    if (aHrefNode == null)
                    {
                        continue;
                    }

                    var code = aHrefNode.InnerText;
                    var title = HtmlEntity.DeEntitize(cells[TableColumnTitle].InnerText).NormalizePascalCase();
                    var link = "https://github.com" + aHrefNode.Attributes[0].Value;
                    var description = cells[TableColumnDescription].InnerText;

                    data.Rules.Add(
                        new Rule(
                            code,
                            title,
                            link,
                            category,
                            description));
                }
            }

            return data;
        }
    }
}