// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
namespace Atc.CodingRules.AnalyzerProviders.Providers;

public partial class StyleCopAnalyzersProvider : AnalyzerProviderBase
{
    private const int TableColumnId = 0;
    private const int TableColumnTitle = 1;
    private const int TableColumnDescription = 2;

    [GeneratedRegex(@"\[(?<code>[A-Z]+\d+[A-Z]*)\]\((?<link>[^)]+)\)", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex RuleIdRegex();

    [GeneratedRegex(@"###\s+(?<category>.+)", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex CategoryRegex();

    public StyleCopAnalyzersProvider(
        ILogger logger,
        bool logWithAnsiConsoleMarkup = false)
        : base(
            logger,
            logWithAnsiConsoleMarkup)
    {
    }

    public static string Name => "StyleCop.Analyzers";

    public override Uri? DocumentationLink { get; set; } = new("https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/DOCUMENTATION.md", UriKind.Absolute);

    private static Uri RawContentBaseUri { get; } = new("https://raw.githubusercontent.com/DotNetAnalyzers/StyleCopAnalyzers/master/", UriKind.Absolute);

    protected override AnalyzerProviderBaseRuleData CreateData()
        => new(Name);

    protected override async Task ReCollect(AnalyzerProviderBaseRuleData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var ruleFiles = new[]
        {
            "documentation/SpecialRules.md",
            "documentation/SpacingRules.md",
            "documentation/ReadabilityRules.md",
            "documentation/OrderingRules.md",
            "documentation/NamingRules.md",
            "documentation/MaintainabilityRules.md",
            "documentation/LayoutRules.md",
            "documentation/DocumentationRules.md",
            "documentation/AlternativeRules.md",
        };

        using var httpClient = new HttpClient();
        foreach (var rulePath in ruleFiles)
        {
            var rules = await GetRulesFromMarkdown(
                rulePath,
                httpClient);
            foreach (var rule in rules)
            {
                data.Rules.Add(rule);
            }
        }
    }

    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "OK.")]
    private static async Task<List<Rule>> GetRulesFromMarkdown(
        string rulePath,
        HttpClient httpClient)
    {
        var linkUri = new Uri(RawContentBaseUri, rulePath);
        var markdown = await httpClient
            .GetStringAsync(linkUri)
            .ConfigureAwait(false);

        var category = ExtractCategoryFromMarkdown(markdown);
        var tableRows = ExtractTableRowsFromMarkdown(markdown);

        var i = category.IndexOf(" Rules", StringComparison.Ordinal);
        if (i > 0)
        {
            category = category[..i];
        }

        var baseUrl = "/DotNetAnalyzers/StyleCopAnalyzers/blob/master/documentation/";

        var rules = new List<Rule>();
        foreach (var row in tableRows)
        {
            var columns = row.Split('|', StringSplitOptions.TrimEntries);
            if (columns.Length < 3)
            {
                continue;
            }

            var idColumn = columns[TableColumnId];
            var match = RuleIdRegex().Match(idColumn);
            if (!match.Success)
            {
                continue;
            }

            var code = match.Groups["code"].Value;
            var relativeLink = match.Groups["link"].Value;
            var titleTrimmed = columns[TableColumnTitle].Trim();
            var title = titleTrimmed.NormalizePascalCase();
            var description = columns[TableColumnDescription].Trim();
            var helpLink = $"https://github.com{baseUrl}{relativeLink}";

            rules.Add(
                new Rule(
                    code,
                    title,
                    helpLink,
                    category,
                    description));
        }

        return rules;
    }

    private static string ExtractCategoryFromMarkdown(string markdown)
    {
        var match = CategoryRegex().Match(markdown);
        return match.Success ? match.Groups["category"].Value.Trim() : "Unknown";
    }

    private static List<string> ExtractTableRowsFromMarkdown(string markdown)
    {
        var lines = markdown.Split('\n');
        var tableRows = new List<string>();
        var inTable = false;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (trimmedLine.Contains('|', StringComparison.Ordinal))
            {
                inTable = true;
                if (!trimmedLine.Contains("---", StringComparison.Ordinal) &&
                    !trimmedLine.Contains("Identifier", StringComparison.OrdinalIgnoreCase))
                {
                    tableRows.Add(trimmedLine);
                }
            }
            else if (inTable && !string.IsNullOrWhiteSpace(trimmedLine))
            {
                break;
            }
        }

        return tableRows;
    }
}