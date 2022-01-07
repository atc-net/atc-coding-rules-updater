namespace Atc.CodingRules.AnalyzerProviders.Providers;

public class SonarAnalyzerCSharpProvider : AnalyzerProviderBase
{
    public SonarAnalyzerCSharpProvider(ILogger logger)
        : base(logger)
    {
    }

    public static string Name => "SonarAnalyzer.CSharp";

    public Uri? RuleLinkBase { get; set; } = new ("https://rules.sonarsource.com/csharp/", UriKind.Absolute);

    public override Uri? DocumentationLink { get; set; } = new ("https://rules.sonarsource.com/page-data/csharp/page-data.json", UriKind.Absolute);

    protected override AnalyzerProviderBaseRuleData CreateData()
        => new (Name);

    protected override async Task ReCollect(
        AnalyzerProviderBaseRuleData data)
    {
        var web = new HtmlWeb();
        var htmlDoc = await web.LoadFromWebAsync(DocumentationLink!.AbsoluteUri).ConfigureAwait(false);
        var jsonDoc = JsonDocument.Parse(htmlDoc.DocumentNode.InnerText);
        var jsonDocItems = jsonDoc.RootElement.GetProperty("result").GetProperty("pageContext").GetProperty("rules").EnumerateArray();

        while (jsonDocItems.MoveNext())
        {
            var jsonElement = jsonDocItems.Current;
            var ruleKey = jsonElement.GetProperty("ruleKey").GetString();
            var summary = jsonElement.GetProperty("summary").GetString() ?? string.Empty;
            var link = RuleLinkBase + ruleKey;
            var description = jsonElement.GetProperty("description").GetString();

            if (ruleKey is null)
            {
                continue;
            }

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