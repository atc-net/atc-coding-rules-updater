using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Atc.CodingRules.AnalyzerProviders.Models;
using HtmlAgilityPack;

namespace Atc.CodingRules.AnalyzerProviders.Providers
{
    public class MeziantouProvider : AnalyzerProviderBase
    {
        private const int TableColumnId = 0;
        private const int TableColumnCategory = 1;
        private const int TableColumnTitle = 2;

        [SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification = "OK.")]
        public override Uri? DocumentationLink { get; set; } = new Uri("https://github.com/meziantou/Meziantou.Analyzer/tree/main/docs", UriKind.Absolute);

        public override AnalyzerProviderBaseRuleData CollectBaseRules()
        {
            var data = new AnalyzerProviderBaseRuleData("Meziantou.Analyzer");

            var web = new HtmlWeb();
            var htmlDoc = web.Load(DocumentationLink);
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
                var link = aHrefNode.Attributes[0].Value;
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