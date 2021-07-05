using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

        public override Uri? DocumentationLink { get; set; } = new Uri("https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/DOCUMENTATION.md", UriKind.Absolute);

        public override async Task<AnalyzerProviderBaseRuleData> CollectBaseRules()
        {
            var data = new AnalyzerProviderBaseRuleData("StyleCop.Analyzers");

            var web = new HtmlWeb();
            var htmlDoc = await web.LoadFromWebAsync(DocumentationLink!.AbsoluteUri).ConfigureAwait(false);
            var articleNode = htmlDoc.DocumentNode.SelectNodes("//article[@class='markdown-body entry-content container-lg']").First();
            var articleRuleLinks = articleNode.SelectNodes("//*//strong//a").ToList();

            var ruleSetTasks = new List<Task<List<Rule>>>();
            foreach (var item in articleRuleLinks)
            {
                if (item.Attributes.Count != 1 ||
                    !item.InnerText.Contains("(S", StringComparison.Ordinal))
                {
                    continue;
                }

                var ruleSetTask = GetRules(item);
                ruleSetTasks.Add(ruleSetTask);
            }

            await Task.WhenAll(ruleSetTasks.ToArray());
            foreach (var ruleSetTask in ruleSetTasks)
            {
                var ruleSet = await ruleSetTask;
                foreach (var rule in ruleSet)
                {
                    data.Rules.Add(rule);
                }
            }

            return data;
        }

        [SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "OK.")]
        [SuppressMessage("Security", "SCS0005:Weak random generator", Justification = "OK.")]
        private static async Task<List<Rule>> GetRules(HtmlNode item)
        {
            // Try not to look like DDoS-attack
            var rnd = new Random();
            int nextMs = rnd.Next(500, 1000);
            await Task.Delay(nextMs);

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