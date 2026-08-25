namespace Atc.CodingRules.AnalyzerProviders.Providers;

/// <summary>
/// Scrapes Meziantou.Analyzer rules from the README table on GitHub.
/// </summary>
/// <remarks>
/// Source: https://github.com/meziantou/Meziantou.Analyzer/tree/main/docs.
/// Path: <c>payload.tree.readme.richText</c> from the GitHub embedded JSON
/// → <c>article[@class='markdown-body entry-content container-lg']</c>
/// → first <c>table</c> → <c>tr</c> rows where columns are
/// (Id link, Category, Title).
/// On structural breakage the base class falls back to the prior snapshot.
/// </remarks>
public class MeziantouProvider : AnalyzerProviderBase
{
    private const int TableColumnId = 0;
    private const int TableColumnCategory = 1;
    private const int TableColumnTitle = 2;

    public MeziantouProvider(
        ILogger logger,
        bool logWithAnsiConsoleMarkup = false)
        : base(
            logger,
            logWithAnsiConsoleMarkup)
    {
    }

    public static string Name => "Meziantou.Analyzer";

    public override Uri? DocumentationLink { get; set; } = new("https://github.com/meziantou/Meziantou.Analyzer/tree/main/docs", UriKind.Absolute);

    protected override AnalyzerProviderBaseRuleData CreateData()
        => new(Name);

    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "OK.")]
    protected override async Task ReCollect(AnalyzerProviderBaseRuleData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var web = new HtmlWeb();
        var htmlDoc = await web
            .LoadFromWebAsync(DocumentationLink!.AbsoluteUri)
            .ConfigureAwait(false);

        var embeddedNode = htmlDoc.DocumentNode.SelectSingleNode("//script[@data-target='react-app.embeddedData']");
        if (embeddedNode is not null)
        {
            var dynamicJson = new DynamicJson(embeddedNode.InnerText);
            var html = dynamicJson.GetValue("payload.tree.readme.richText")?.ToString();

            if (html is null)
            {
                return;
            }

            htmlDoc.LoadHtml(html);
        }

        var articleNodes = htmlDoc.DocumentNode.SelectNodes("//article[@class='markdown-body entry-content container-lg']");
        if (articleNodes is null || articleNodes.Count == 0)
        {
            data.ExceptionMessage = "Could not locate the documentation article on the page.";
            return;
        }

        var articleNode = articleNodes[0];

        var articleTableRows = articleNode
            .SelectNodes("//*//table[1]//tr")
            ?.ToList();

        if (articleTableRows is null)
        {
            data.ExceptionMessage = "Could not locate the documentation table on the page.";
            return;
        }

        foreach (var row in articleTableRows)
        {
            var cellNodes = row.SelectNodes("td");
            if (cellNodes is null)
            {
                continue;
            }

            var cells = cellNodes.ToList();

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
            var link = aHrefNode.Attributes["href"]?.Value;
            var category = cells[TableColumnCategory].InnerText;

            data.Rules.Add(
                new Rule(
                    code,
                    title,
                    link ?? string.Empty,
                    category: category));
        }
    }
}