namespace Atc.CodingRules.AnalyzerProviders.Providers;

public class XunitProvider : AnalyzerProviderBase
{
    private const int TableThColumnId = 0;
    private const int TableTdColumnCategory = 2;
    private const int TableTdColumnTitle = 0;

    public XunitProvider(
        ILogger logger,
        bool logWithAnsiConsoleMarkup = false)
        : base(
            logger,
            logWithAnsiConsoleMarkup)
    {
    }

    public static string Name => "xUnit.net";

    public override Uri? DocumentationLink { get; set; } = new ("https://xunit.net/xunit.analyzers/rules", UriKind.Absolute);

    protected override AnalyzerProviderBaseRuleData CreateData()
        => new (Name);

    protected override async Task ReCollect(
        AnalyzerProviderBaseRuleData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var web = new HtmlWeb();
        var htmlDoc = await web.LoadFromWebAsync(DocumentationLink!.AbsoluteUri).ConfigureAwait(false);
        var articleNode = htmlDoc.DocumentNode.SelectNodes("//table[@class='table']").First();
        var articleTableRows = articleNode.SelectNodes("//*//tr").ToList();

        foreach (var row in articleTableRows)
        {
            if (row.SelectNodes("th") is null ||
                row.SelectNodes("td") is null)
            {
                continue;
            }

            var cellsTh = row.SelectNodes("th").ToList();
            var cellsTd = row.SelectNodes("td").ToList();
            if (cellsTh.Count <= 0 || cellsTd.Count <= 0)
            {
                continue;
            }

            var aHrefNode = cellsTh[TableThColumnId].SelectSingleNode("a");
            if (aHrefNode is null)
            {
                continue;
            }

            var code = aHrefNode.InnerText.RemoveNewLines().Trim();
            var title = HtmlEntity.DeEntitize(cellsTd[TableTdColumnTitle].InnerText);
            var link = $"{DocumentationLink}/{code}";
            var category = cellsTd[TableTdColumnCategory].InnerText;

            data.Rules.Add(
                new Rule(
                    code,
                    title,
                    link,
                    category: category));
        }
    }
}
