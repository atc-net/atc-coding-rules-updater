using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Atc.CodingRules.AnalyzerProviders.Models;
using HtmlAgilityPack;

namespace Atc.CodingRules.AnalyzerProviders.Providers
{
    public class SecurityCodeScanVs2019Provider : AnalyzerProviderBase
    {
        [SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification = "OK.")]
        public override Uri? DocumentationLink { get; set; } = new Uri("https://security-code-scan.github.io", UriKind.Absolute);

        public override AnalyzerProviderBaseRuleData CollectBaseRules()
        {
            var data = new AnalyzerProviderBaseRuleData("SecurityCodeScan.VS2019");

            var web = new HtmlWeb();
            var htmlDoc = web.Load(DocumentationLink);
            var headers3 = htmlDoc.DocumentNode.SelectNodes("//h3").ToList();

            foreach (var item in headers3)
            {
                if (!item.InnerText.Contains("SCS", StringComparison.Ordinal))
                {
                    continue;
                }

                var sa = item.InnerText.Split(" - ");

                var code = sa[0];
                var title = sa[1];
                var link = this.DocumentationLink!.OriginalString + $"#{code}";

                data.Rules.Add(
                    new Rule(
                        code,
                        title,
                        link));
            }

            return data;
        }
    }
}