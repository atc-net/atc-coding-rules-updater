namespace Atc.CodingRules.AnalyzerProviders.Providers;

public class AsyncFixerProvider : AnalyzerProviderBase
{
    public AsyncFixerProvider(ILogger logger, bool logWithAnsiConsoleMarkup = false)
        : base(logger, logWithAnsiConsoleMarkup)
    {
    }

    public static string Name => "AsyncFixer";

    public override Uri? DocumentationLink { get; set; } = new ("https://github.com/semihokur/AsyncFixer/blob/main/README.md", UriKind.Absolute);

    protected override AnalyzerProviderBaseRuleData CreateData()
        => new (Name);

    protected override async Task ReCollect(
        AnalyzerProviderBaseRuleData data)
    {
        var web = new HtmlWeb();
        var htmlDoc = await web.LoadFromWebAsync(DocumentationLink!.AbsoluteUri);

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
            var link = $"{this.DocumentationLink.OriginalString}#{hashTagId}";

            data.Rules.Add(
                new Rule(
                    code,
                    title,
                    link,
                    description: description));
        }
    }
}