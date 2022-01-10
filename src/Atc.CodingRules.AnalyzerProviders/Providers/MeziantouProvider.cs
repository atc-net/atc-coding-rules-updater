namespace Atc.CodingRules.AnalyzerProviders.Providers;

public class MeziantouProvider : AnalyzerProviderBase
{
    private const int TableColumnId = 0;
    private const int TableColumnCategory = 1;
    private const int TableColumnTitle = 2;

    public MeziantouProvider(ILogger logger, bool logWithAnsiConsoleMarkup = false)
        : base(logger, logWithAnsiConsoleMarkup)
    {
    }

    public static string Name => "Meziantou.Analyzer";

    public override Uri? DocumentationLink { get; set; } = new ("https://github.com/meziantou/Meziantou.Analyzer/tree/main/docs", UriKind.Absolute);

    protected override AnalyzerProviderBaseRuleData CreateData()
        => new (Name);

    protected override async Task ReCollect(
        AnalyzerProviderBaseRuleData data)
    {
        var web = new HtmlWeb();
        var htmlDoc = await web.LoadFromWebAsync(DocumentationLink!.AbsoluteUri).ConfigureAwait(false);
        var articleNode = htmlDoc.DocumentNode.SelectNodes("//article[@class='markdown-body entry-content container-lg']").First();
        var articleTableRows = articleNode.SelectNodes("//*//table[1]//tr").ToList();

        foreach (var row in articleTableRows)
        {
            if (row.SelectNodes("td") is null)
            {
                continue;
            }

            var cells = row.SelectNodes("td").ToList();
            if (cells.Count <= 0)
            {
                continue;
            }

            var aHrefNode = cells[TableColumnId].SelectSingleNode("a");
            if (aHrefNode is null)
            {
                continue;
            }

            var code = aHrefNode.InnerText;
            var title = HtmlEntity.DeEntitize(cells[TableColumnTitle].InnerText);
            var link = aHrefNode.Attributes["href"].Value;
            var category = cells[TableColumnCategory].InnerText;

            data.Rules.Add(
                new Rule(
                    code,
                    title,
                    link,
                    category: category));
        }
    }
}