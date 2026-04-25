namespace Atc.CodingRules.AnalyzerProviders.Providers;

/// <summary>
/// Scrapes AsyncFixer rules from the project README on GitHub.
/// </summary>
/// <remarks>
/// Source: https://github.com/semihokur/AsyncFixer/blob/main/README.md.
/// Primary path: GitHub embeds the rendered README JSON in
/// <c>&lt;script data-target="react-app.embeddedData"&gt;</c>; we read
/// <c>payload.blob.headerInfo.toc</c> for the rule list (each entry's <c>text</c>
/// is "AsyncFixerNN: Title").
/// Fallback: traverse <c>//h3</c> headings whose text starts with "Async".
/// On structural breakage <see cref="Models.AnalyzerProviderBaseRuleData.ExceptionMessage"/> is set
/// and the prior cached snapshot is reused by the base class.
/// </remarks>
public class AsyncFixerProvider : AnalyzerProviderBase
{
    public AsyncFixerProvider(
        ILogger logger,
        bool logWithAnsiConsoleMarkup = false)
        : base(
            logger,
            logWithAnsiConsoleMarkup)
    {
    }

    public static string Name => "AsyncFixer";

    public override Uri? DocumentationLink { get; set; } = new("https://github.com/semihokur/AsyncFixer/blob/main/README.md", UriKind.Absolute);

    protected override AnalyzerProviderBaseRuleData CreateData()
        => new(Name);

    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "OK.")]
    protected override async Task ReCollect(AnalyzerProviderBaseRuleData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var web = new HtmlWeb();
        var htmlDoc = await web.LoadFromWebAsync(DocumentationLink!.AbsoluteUri);

        var embeddedNode = htmlDoc.DocumentNode.SelectSingleNode("//script[@data-target='react-app.embeddedData']");
        if (embeddedNode is not null)
        {
            var dynamicJson = new DynamicJson(embeddedNode.InnerText);
            if (dynamicJson.GetValue("payload.blob.headerInfo.toc") is List<object> tocObjects)
            {
                foreach (var tocObject in tocObjects)
                {
                    if (tocObject is not Dictionary<string, object> tocItem)
                    {
                        continue;
                    }

                    if (!tocItem.TryGetValue("text", out var value))
                    {
                        continue;
                    }

                    var sa = value
                        .ToString()!
                        .Split(':', StringSplitOptions.RemoveEmptyEntries);

                    if (sa.Length != 2)
                    {
                        continue;
                    }

                    var code = sa[0].Trim();
                    var title = sa[1].Trim();

                    data.Rules.Add(
                        new Rule(
                            code,
                            title,
                            link: string.Empty,
                            description: title));
                }

                return;
            }
        }

        var headers3 = htmlDoc.DocumentNode
            .SelectNodes("//h3")
            ?.ToList();

        if (headers3 is null)
        {
            data.ExceptionMessage = "Could not locate any rule headings on the page.";
            return;
        }

        foreach (var item in headers3)
        {
            if (!item.InnerText.StartsWith("Async", StringComparison.Ordinal))
            {
                continue;
            }

            var description = item.NextSibling?.NextSibling?.InnerText
                .Replace(" Here is an example:", string.Empty, StringComparison.OrdinalIgnoreCase)
                ?? string.Empty;

            var sa = item.InnerText.Split(':');
            if (sa.Length != 2)
            {
                continue;
            }

            var code = sa[0];
            var title = sa[1].Trim();
            var hashTagId = $"user-content-{code.ToLower(GlobalizationConstants.EnglishCultureInfo)}{title
                .ToLower(GlobalizationConstants.EnglishCultureInfo)
                .Replace(" ", "-", StringComparison.Ordinal)
                .Replace("/", string.Empty, StringComparison.Ordinal)
                .Replace(".", string.Empty, StringComparison.Ordinal)}";

            var link = $"{DocumentationLink.OriginalString}#{hashTagId}";

            data.Rules.Add(
                new Rule(
                    code,
                    title,
                    link,
                    description: description));
        }
    }
}