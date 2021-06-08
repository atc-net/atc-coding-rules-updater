using System;
using System.Linq;
using System.Threading.Tasks;
using Atc.CodingRules.AnalyzerProviders.Models;
using HtmlAgilityPack;

namespace Atc.CodingRules.AnalyzerProviders.Providers
{
    public class SecurityCodeScanVs2019Provider : AnalyzerProviderBase
    {
        public override Uri? DocumentationLink { get; set; } = new Uri("https://security-code-scan.github.io", UriKind.Absolute);

        public override async Task<AnalyzerProviderBaseRuleData> CollectBaseRules()
        {
            var data = new AnalyzerProviderBaseRuleData("SecurityCodeScan.VS2019");

            var web = new HtmlWeb();
            var htmlDoc = await web.LoadFromWebAsync(DocumentationLink!.AbsoluteUri).ConfigureAwait(false);
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
                var link = $"{this.DocumentationLink!.OriginalString}#{code}";

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