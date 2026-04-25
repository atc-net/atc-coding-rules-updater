namespace Atc.CodingRules.AnalyzerProviders.Providers;

/// <summary>
/// Scrapes WpfAnalyzers rules from the project README on GitHub.
/// </summary>
/// <remarks>
/// Source: https://github.com/DotNetAnalyzers/WpfAnalyzers.
/// Path: first <c>article[@class='markdown-body entry-content container-lg']</c>
/// → first <c>table</c> → <c>tr</c> rows where columns are (Id link, Title).
/// </remarks>
public class WpfAnalyzersProvider : AnalyzerProviderBase
{
    private const int TableColumnId = 0;
    private const int TableColumnTitle = 1;

    public WpfAnalyzersProvider(
        ILogger logger,
        bool logWithAnsiConsoleMarkup = false)
        : base(
            logger,
            logWithAnsiConsoleMarkup)
    {
    }

    public static string Name => "WpfAnalyzers";

    public override Uri? DocumentationLink { get; set; } = new("https://github.com/DotNetAnalyzers/WpfAnalyzers", UriKind.Absolute);

    protected override AnalyzerProviderBaseRuleData CreateData()
        => new(Name);

    protected override async Task ReCollect(AnalyzerProviderBaseRuleData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var web = new HtmlWeb();
        var htmlDoc = await web
            .LoadFromWebAsync(DocumentationLink!.AbsoluteUri)
            .ConfigureAwait(false);

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
            if (row.SelectNodes("td") is null)
            {
                continue;
            }

            var cells = row
                .SelectNodes("td")
                .ToList();

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

            data.Rules.Add(
                new Rule(
                    code,
                    title,
                    link));
        }
    }
}