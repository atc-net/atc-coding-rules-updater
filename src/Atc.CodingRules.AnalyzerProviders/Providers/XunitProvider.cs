namespace Atc.CodingRules.AnalyzerProviders.Providers;

public class XunitProvider : AnalyzerProviderBase
{
    private const int TableColumnId = 0;
    private const int TableColumnTitle = 3;

    public XunitProvider(
        ILogger logger,
        bool logWithAnsiConsoleMarkup = false)
        : base(
            logger,
            logWithAnsiConsoleMarkup)
    {
    }

    public static string Name => "xUnit.net";

    public override Uri? DocumentationLink { get; set; } = new("https://xunit.net/xunit.analyzers/rules", UriKind.Absolute);

    protected override AnalyzerProviderBaseRuleData CreateData()
        => new(Name);

    protected override async Task ReCollect(AnalyzerProviderBaseRuleData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var web = new HtmlWeb();
        var htmlDoc = await web
            .LoadFromWebAsync(DocumentationLink!.AbsoluteUri)
            .ConfigureAwait(false);

        var tables = htmlDoc.DocumentNode.SelectNodes("//table");
        if (tables is null || tables.Count == 0)
        {
            return;
        }

        var articleTableRows = new List<HtmlNode>();
        foreach (var table in tables)
        {
            var rows = table.SelectNodes(".//tr");
            if (rows is not null)
            {
                articleTableRows.AddRange(rows);
            }
        }

        foreach (var row in articleTableRows)
        {
            var rule = TryParseRuleFromRow(row);
            if (rule is not null)
            {
                data.Rules.Add(rule);
            }
        }
    }

    private Rule? TryParseRuleFromRow(HtmlNode row)
    {
        var cells = row.SelectNodes("td");
        if (cells is null || cells.Count <= TableColumnTitle)
        {
            return null;
        }

        var cellsList = cells.ToList();
        var aHrefNode = cellsList[TableColumnId].SelectSingleNode("a");
        if (aHrefNode is null)
        {
            return null;
        }

        var code = aHrefNode.InnerText
            .RemoveNewLines()
            .Trim();
        var title = HtmlEntity.DeEntitize(cellsList[TableColumnTitle].InnerText);
        var link = $"{DocumentationLink}/{code}";

        return new Rule(code, title, link, category: null);
    }
}