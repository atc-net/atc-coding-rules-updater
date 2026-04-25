namespace Atc.CodingRules.AnalyzerProviders.Providers;

/// <summary>
/// Scrapes Microsoft .NET CA-prefix rule definitions from Microsoft Learn.
/// </summary>
/// <remarks>
/// Source: https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules.
/// Path: first <c>table</c> → <c>tr</c> rows where the first cell holds an anchor
/// whose text is <c>"CAxxxx: Title"</c> (or "<c>CAxxxx Title</c>"), the second cell holds the category.
/// Detects the Microsoft Learn "Access Denied" page and short-circuits to a clear error message.
/// </remarks>
public class MicrosoftCodeAnalysisNetAnalyzersProvider : AnalyzerProviderBase
{
    private const int TableColumnId = 0;
    private const int TableColumnCategory = 1;

    public MicrosoftCodeAnalysisNetAnalyzersProvider(
        ILogger logger,
        bool logWithAnsiConsoleMarkup = false)
        : base(
            logger,
            logWithAnsiConsoleMarkup)
    {
    }

    public static string Name => "Microsoft.CodeAnalysis.NetAnalyzers";

    public override Uri? DocumentationLink { get; set; } = new("https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules", UriKind.Absolute);

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

        if (htmlDoc.DocumentNode.HasTitleWithAccessDenied())
        {
            data.ExceptionMessage = "Access Denied";
            return;
        }

        var tableRows = htmlDoc.DocumentNode
            .SelectNodes("//*//table[1]//tr")
            ?.ToList();

        if (tableRows is null)
        {
            data.ExceptionMessage = "Could not locate the documentation table on the page.";
            return;
        }

        foreach (var row in tableRows)
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

            var sa = aHrefNode.InnerText.Split(":");
            if (sa.Length != 2)
            {
                sa = aHrefNode.InnerText.Split([' '], 2);
                if (sa.Length != 2)
                {
                    continue;
                }
            }

            var code = sa[0];
            var title = sa[1].Trim();
            var description = HtmlEntity.DeEntitize(cells[TableColumnCategory].InnerText);
            var link = $"{DocumentationLink.OriginalString}/{aHrefNode.Attributes["href"].Value}";

            data.Rules.Add(
                new Rule(
                    code,
                    title,
                    link,
                    category: null,
                    description));
        }
    }
}