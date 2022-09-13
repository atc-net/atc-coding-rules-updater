namespace Atc.CodingRules.AnalyzerProviders.Providers;

public class SecurityCodeScanVs2019Provider : AnalyzerProviderBase
{
    public SecurityCodeScanVs2019Provider(
        ILogger logger,
        bool logWithAnsiConsoleMarkup = false)
        : base(
            logger,
            logWithAnsiConsoleMarkup)
    {
    }

    public static string Name => "SecurityCodeScan.VS2019";

    public override Uri? DocumentationLink { get; set; } = new ("https://security-code-scan.github.io", UriKind.Absolute);

    protected override AnalyzerProviderBaseRuleData CreateData()
        => new (Name);

    protected override async Task ReCollect(
        AnalyzerProviderBaseRuleData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var web = new HtmlWeb();
        var htmlDoc = await web.LoadFromWebAsync(DocumentationLink!.AbsoluteUri).ConfigureAwait(false);
        var headers3 = htmlDoc.DocumentNode.SelectNodes("//h3").ToList();

        foreach (var item in headers3.Select(x => x.InnerText))
        {
            if (!item.Contains("SCS", StringComparison.Ordinal))
            {
                continue;
            }

            var sa = item.Split(" - ");
            if (sa.Length != 2)
            {
                continue;
            }

            var code = sa[0];
            var title = sa[1];
            var link = $"{DocumentationLink!.OriginalString}#{code}";

            data.Rules.Add(
                new Rule(
                    code,
                    title,
                    link));
        }
    }
}