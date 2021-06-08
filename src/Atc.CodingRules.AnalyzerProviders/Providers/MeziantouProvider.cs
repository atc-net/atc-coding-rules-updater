using System;
using System.Linq;
using System.Threading.Tasks;
using Atc.CodingRules.AnalyzerProviders.Models;
using HtmlAgilityPack;

namespace Atc.CodingRules.AnalyzerProviders.Providers
{
    public class MeziantouProvider : AnalyzerProviderBase
    {
        private const int TableColumnId = 0;
        private const int TableColumnCategory = 1;
        private const int TableColumnTitle = 2;

        public override Uri? DocumentationLink { get; set; } = new Uri("https://github.com/meziantou/Meziantou.Analyzer/tree/main/docs", UriKind.Absolute);

        public override async Task<AnalyzerProviderBaseRuleData> CollectBaseRules()
        {
            var data = new AnalyzerProviderBaseRuleData("Meziantou.Analyzer");

            var web = new HtmlWeb();
            var htmlDoc = await web.LoadFromWebAsync(DocumentationLink!.AbsoluteUri).ConfigureAwait(false);
            var articleNode = htmlDoc.DocumentNode.SelectNodes("//article[@class='markdown-body entry-content container-lg']").First();
            var articleTableRows = articleNode.SelectNodes("//*//table[1]//tr").ToList();

            foreach (var row in articleTableRows)
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
                var title = HtmlEntity.DeEntitize(cells[TableColumnTitle].InnerText);
                var link = aHrefNode.Attributes["href"].Value;
                var category = cells[TableColumnCategory].InnerText;

                data.Rules.Add(
                    new Rule(
                        code,
                        title,
                        link,
                        category: category));
            }

            return data;
        }
    }
}