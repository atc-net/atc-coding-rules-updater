namespace Atc.CodingRules.AnalyzerProviders.Providers;

public class SonarAnalyzerCSharpProvider : AnalyzerProviderBase
{
    public SonarAnalyzerCSharpProvider(
        ILogger logger,
        bool logWithAnsiConsoleMarkup = false)
        : base(
            logger,
            logWithAnsiConsoleMarkup)
    {
    }

    public static string Name => "SonarAnalyzer.CSharp";

    public Uri? RuleLinkBase { get; set; } = new("https://rules.sonarsource.com/csharp/", UriKind.Absolute);

    public override Uri? DocumentationLink { get; set; } = new("https://rules.sonarsource.com/page-data/csharp/page-data.json", UriKind.Absolute);

    protected override AnalyzerProviderBaseRuleData CreateData()
        => new(Name);

    protected override async Task ReCollect(
        AnalyzerProviderBaseRuleData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var web = new HtmlWeb();
        var htmlDoc = await web.LoadFromWebAsync(DocumentationLink!.AbsoluteUri).ConfigureAwait(false);

        var dynamicJson = new DynamicJson(htmlDoc.DocumentNode.InnerText);
        if (dynamicJson.GetValue("result.data.allFile.nodes") is not List<object> nodes)
        {
            return;
        }

        if (nodes[0] is not Dictionary<string, object> node)
        {
            return;
        }

        if (node["childLanguageJson"] is not Dictionary<string, object> childLanguageJson)
        {
            return;
        }

        if (childLanguageJson["rules"] is not List<object> rules)
        {
            return;
        }

        foreach (var ruleObj in rules)
        {
            if (ruleObj is not Dictionary<string, object> ruleDict)
            {
                continue;
            }

            var ruleKey = ruleDict["ruleKey"].ToString();
            var summary = ruleDict["summary"].ToString() ?? string.Empty;
            var link = RuleLinkBase + ruleKey;
            var description = ruleDict["description"].ToString();

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