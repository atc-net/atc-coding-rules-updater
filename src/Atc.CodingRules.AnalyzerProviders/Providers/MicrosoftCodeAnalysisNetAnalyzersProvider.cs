using System;
using System.Linq;
using System.Threading.Tasks;
using Atc.CodingRules.AnalyzerProviders.Models;
using HtmlAgilityPack;

namespace Atc.CodingRules.AnalyzerProviders.Providers
{
    public class MicrosoftCodeAnalysisNetAnalyzersProvider : AnalyzerProviderBase
    {
        private const int TableColumnId = 0;
        private const int TableColumnCategory = 1;

        public override Uri? DocumentationLink { get; set; } = new Uri("https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules", UriKind.Absolute);

        public override async Task<AnalyzerProviderBaseRuleData> CollectBaseRules()
        {
            var data = new AnalyzerProviderBaseRuleData("Microsoft.CodeAnalysis.NetAnalyzers");

            var web = new HtmlWeb();
            var htmlDoc = await web.LoadFromWebAsync(DocumentationLink!.AbsoluteUri).ConfigureAwait(false);
            if (htmlDoc.DocumentNode.HasTitleWithAccessDenied())
            {
                data.ExceptionMessage = "Access Denied";
                return data;
            }

            var tableRows = htmlDoc.DocumentNode.SelectNodes("//*//table[1]//tr").ToList();

            foreach (var row in tableRows)
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

                var sa = aHrefNode.InnerText.Split(":");
                if (sa.Length != 2)
                {
                    sa = aHrefNode.InnerText.Split(new[] { ' ' }, 2);
                    if (sa.Length != 2)
                    {
                        continue;
                    }
                }

                var code = sa[0];
                var title = sa[1].Trim();
                var description = HtmlEntity.DeEntitize(cells[TableColumnCategory].InnerText);
                var link = $"{this.DocumentationLink.OriginalString}/{aHrefNode.Attributes["href"].Value}";

                data.Rules.Add(
                    new Rule(
                        code,
                        title,
                        link,
                        category: null,
                        description));
            }

            return data;
        }
    }
}