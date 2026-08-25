namespace Atc.CodingRules.AnalyzerProviders.Providers;

/// <summary>
/// Scrapes Microsoft.VisualStudio.Threading.Analyzers (VSTHRDxxx) rules from the docs index page.
/// </summary>
/// <remarks>
/// Source: https://microsoft.github.io/vs-threading/analyzers/index.html.
/// Path: first <c>table</c> → <c>.//tr</c> rows; columns are (Id link, Title, Category).
/// </remarks>
public class MicrosoftVisualStudioThreadingAnalyzersProvider : AnalyzerProviderBase
{
    private const int TableColumnId = 0;
    private const int TableColumnCategory = 2;
    private const int TableColumnTitle = 1;

    public MicrosoftVisualStudioThreadingAnalyzersProvider(
        ILogger logger,
        bool logWithAnsiConsoleMarkup = false)
        : base(
            logger,
            logWithAnsiConsoleMarkup)
    {
    }

    public static string Name => "Microsoft.VisualStudio.Threading.Analyzers";

    public override Uri? DocumentationLink { get; set; } = new("https://microsoft.github.io/vs-threading/analyzers/index.html", UriKind.Absolute);

    protected override AnalyzerProviderBaseRuleData CreateData()
        => new(Name);

    protected override async Task ReCollect(AnalyzerProviderBaseRuleData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var web = new HtmlWeb();
        var htmlDoc = await web
            .LoadFromWebAsync(DocumentationLink!.AbsoluteUri)
            .ConfigureAwait(false);

        var tableNode = htmlDoc.DocumentNode.SelectSingleNode("//table");
        if (tableNode is null)
        {
            return;
        }

        var articleTableRows = tableNode
            .SelectNodes(".//tr")?
            .ToList();

        if (articleTableRows is null)
        {
            return;
        }

        foreach (var row in articleTableRows)
        {
            var rule = CreateRuleFromRow(row);
            if (rule is not null)
            {
                data.Rules.Add(rule);
            }
        }
    }

    private static Rule? CreateRuleFromRow(HtmlNode row)
    {
        var cellNodes = row.SelectNodes("td");
        if (cellNodes is null)
        {
            return null;
        }

        var cells = cellNodes.ToList();

        if (cells.Count <= 0)
        {
            return null;
        }

        var aHrefNode = cells[TableColumnId].SelectSingleNode("a");
        if (aHrefNode is null)
        {
            return null;
        }

        var hrefValue = aHrefNode.Attributes["href"]?.Value;
        if (hrefValue is null)
        {
            return null;
        }

        var code = aHrefNode.InnerText;
        var title = HtmlEntity.DeEntitize(cells[TableColumnTitle].InnerText);
        var link = hrefValue.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? hrefValue
            : $"https://microsoft.github.io/vs-threading/analyzers/{hrefValue}";
        var category = cells[TableColumnCategory].InnerText;

        return new Rule(
            code,
            title,
            link,
            category: category);
    }
}