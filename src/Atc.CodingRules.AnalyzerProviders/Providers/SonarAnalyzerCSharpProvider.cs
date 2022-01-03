using System;
using System.Text.Json;
using System.Threading.Tasks;
using Atc.CodingRules.AnalyzerProviders.Models;
using HtmlAgilityPack;

namespace Atc.CodingRules.AnalyzerProviders.Providers
{
    public class SonarAnalyzerCSharpProvider : AnalyzerProviderBase
    {
        public static string Name => "SonarAnalyzer.CSharp";

        public Uri? RuleLinkBase { get; set; } = new Uri("https://rules.sonarsource.com/csharp/", UriKind.Absolute);

        public override Uri? DocumentationLink { get; set; } = new Uri("https://rules.sonarsource.com/page-data/csharp/page-data.json", UriKind.Absolute);

        protected override AnalyzerProviderBaseRuleData CreateData()
        {
            return new AnalyzerProviderBaseRuleData(Name);
        }

        protected override async Task ReCollect(AnalyzerProviderBaseRuleData data)
        {
            var web = new HtmlWeb();
            var htmlDoc = await web.LoadFromWebAsync(DocumentationLink!.AbsoluteUri).ConfigureAwait(false);
            var jsonDoc = JsonDocument.Parse(htmlDoc.DocumentNode.InnerText);
            var jsonDocItems = jsonDoc.RootElement.GetProperty("result").GetProperty("pageContext").GetProperty("rules").EnumerateArray();

            while (jsonDocItems.MoveNext())
            {
                var jsonElement = jsonDocItems.Current;
                var ruleKey = jsonElement.GetProperty("ruleKey").GetRawText();
                var summary = jsonElement.GetProperty("summary").GetRawText();
                var link = RuleLinkBase + ruleKey;
                var description = jsonElement.GetProperty("description").GetRawText();

                var rule = new Rule(
                    ruleKey.Replace("RSPEC-", string.Empty, StringComparison.Ordinal),
                    summary,
                    link,
                    category: null,
                    description);

                data.Rules.Add(rule);
            }
        }
    }
}