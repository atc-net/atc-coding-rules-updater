using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Atc.CodingRules.AnalyzerRulesMetaData.Generator.CLI.Models;
using HtmlAgilityPack;

namespace Atc.CodingRules.AnalyzerRulesMetaData.Generator.CLI.AnalyzerProviders
{
    public class MeziantouProvider : AnalyzerProviderBase
    {
        [SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification = "OK.")]
        public override Uri DocumentationLink { get; set; } = new Uri("https://github.com/meziantou/Meziantou.Analyzer/tree/main/docs", UriKind.Absolute);

        public override AnalyzerProviderData RetrieveData()
        {
            var ap = new AnalyzerProviderData("Meziantou.Analyzer");

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

                var aHrefNode = cells[0].SelectSingleNode("a");

                var code = aHrefNode.InnerText;
                var title = cells[2].InnerText;
                var link = aHrefNode.Attributes[0].Value;

                ap.Rules.Add(
                    new Rule(
                        code,
                        title,
                        link));
            }

            return ap;
        }
    }
}