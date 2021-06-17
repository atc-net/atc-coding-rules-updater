using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Atc.CodingRules.AnalyzerProviders.Models;
using HtmlAgilityPack;

namespace Atc.CodingRules.AnalyzerProviders.Providers
{
    public class SonarAnalyzerCSharpProvider : AnalyzerProviderBase
    {
        public override Uri? DocumentationLink { get; set; } = new Uri("https://rules.sonarsource.com/csharp/", UriKind.Absolute);

        public override async Task<AnalyzerProviderBaseRuleData> CollectBaseRules()
        {
            var data = new AnalyzerProviderBaseRuleData("SonarAnalyzer.CSharp");

            HtmlDocument? htmlDoc;
            try
            {
                htmlDoc = WebScrapingHelper.GetPage(this.DocumentationLink!, true);
            }
            catch (IOException ex)
            {
                data.ExceptionMessage = ex.Message;
                return data;
            }

            WebScrapingHelper.QuitWebDriver();
            if (htmlDoc is null)
            {
                return data;
            }

            var listItems = htmlDoc.DocumentNode.SelectNodes("//*//ol[@class='sc-dNLxif dyMlJY']//li").ToList();

            foreach (var node in listItems)
            {
                var aHrefNode = node.SelectSingleNode("a");
                var titleNode = aHrefNode.SelectSingleNode("h3");
                var categoryNode = aHrefNode.SelectSingleNode("span");
                var attributeValue = aHrefNode.Attributes["href"];

                var title = titleNode.InnerText;
                var category = categoryNode.InnerText.Replace("&nbsp;", string.Empty, StringComparison.Ordinal);
                var code = attributeValue.Value.Replace("/csharp/RSPEC-", string.Empty, StringComparison.Ordinal);

                data.Rules.Add(
                    new Rule(
                        $"S{code}",
                        title,
                        $"{DocumentationLink}RSPEC-{code}",
                        category: category));
            }

            await Task.CompletedTask;

            return data;
        }
    }
}