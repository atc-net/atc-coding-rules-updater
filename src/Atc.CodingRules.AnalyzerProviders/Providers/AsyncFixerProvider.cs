namespace Atc.CodingRules.AnalyzerProviders.Providers;

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
    protected override async Task ReCollect(
        AnalyzerProviderBaseRuleData data)
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

                    if (!tocItem.ContainsKey("text"))
                    {
                        continue;
                    }

                    var sa = tocItem["text"]
                        .ToString()!
                        .Split(':', StringSplitOptions.RemoveEmptyEntries);

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

        var headers3 = htmlDoc.DocumentNode.SelectNodes("//h3").ToList();

        foreach (var item in headers3)
        {
            if (!item.InnerText.StartsWith("Async", StringComparison.Ordinal))
            {
                continue;
            }

            var description = item.NextSibling.NextSibling.InnerText.Replace(" Here is an example:", string.Empty, StringComparison.OrdinalIgnoreCase);

            var sa = item.InnerText.Split(':');
            if (sa.Length != 2)
            {
                continue;
            }

            var code = sa[0];
            var title = sa[1].Trim();
            var hashTagId =
                $"user-content-{code.ToLower(GlobalizationConstants.EnglishCultureInfo)}{title.ToLower(GlobalizationConstants.EnglishCultureInfo).Replace(" ", "-", StringComparison.Ordinal).Replace("/", string.Empty, StringComparison.Ordinal).Replace(".", string.Empty, StringComparison.Ordinal)}";
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