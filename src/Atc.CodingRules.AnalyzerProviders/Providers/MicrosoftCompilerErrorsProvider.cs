namespace Atc.CodingRules.AnalyzerProviders.Providers;

public class MicrosoftCompilerErrorsProvider : AnalyzerProviderBase
{
    public MicrosoftCompilerErrorsProvider(
        ILogger logger,
        bool logWithAnsiConsoleMarkup = false)
        : base(
            logger,
            logWithAnsiConsoleMarkup)
    {
    }

    public static string Name => "Microsoft.CompilerErrors";

    public override Uri? DocumentationLink { get; set; } = new("https://learn.microsoft.com/en-us/dotnet/csharp/language-reference", UriKind.Absolute);

    protected override AnalyzerProviderBaseRuleData CreateData()
        => new(Name);

    protected override async Task ReCollect(
        AnalyzerProviderBaseRuleData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var web = new HtmlWeb();
        var htmlDoc = await web.LoadFromWebAsync(DocumentationLink!.AbsoluteUri + "/toc.json").ConfigureAwait(false);
        if (htmlDoc.DocumentNode.HasTitleWithAccessDenied())
        {
            data.ExceptionMessage = "Access Denied";
            return;
        }

        var jsonDoc = JsonDocument.Parse(htmlDoc.DocumentNode.InnerText);
        var jsonDocItems = jsonDoc.RootElement.GetProperty("items").EnumerateArray();

        while (jsonDocItems.MoveNext())
        {
            var jsonElement = jsonDocItems.Current;

            if (!jsonElement.GetRawText().Contains("children", StringComparison.Ordinal))
            {
                continue;
            }

            var tocTitle = jsonElement.GetProperty("toc_title").ToString();
            if (!tocTitle.Equals("C# compiler messages", StringComparison.Ordinal))
            {
                continue;
            }

            var jsonChildItems = jsonElement.GetProperty("children").EnumerateArray();
            while (jsonChildItems.MoveNext())
            {
                var jsonChildElement = jsonChildItems.Current;
                if (jsonChildElement.ValueKind != JsonValueKind.Object ||
                    !jsonChildElement.TryGetProperty("children", out var jsonChildElement2))
                {
                    continue;
                }

                foreach (var element in jsonChildElement2.EnumerateArray())
                {
                    var hrefPart = element.GetProperty("href").ToString();
                    var code = element.GetProperty("toc_title").ToString();

                    var link = hrefPart.StartsWith("../misc/", StringComparison.Ordinal)
                        ? "https://docs.microsoft.com/en-us/dotnet/csharp/" + hrefPart.Replace("../", string.Empty, StringComparison.Ordinal)
                        : DocumentationLink.AbsoluteUri + "/" + hrefPart;

                    var rule = await GetRuleByCode(code, link);
                    if (rule is not null)
                    {
                        data.Rules.Add(rule);
                    }
                }
            }
        }
    }

    private static async Task<Rule?> GetRuleByCode(
        string code,
        string link)
    {
        var web = new HtmlWeb();
        var htmlDoc = await web.LoadFromWebAsync(link).ConfigureAwait(false);
        if (htmlDoc.DocumentNode.HasTitleWithAccessDenied())
        {
            return null;
        }

        var mainNode = htmlDoc.DocumentNode.SelectSingleNode("//main[@id='main']");
        if (mainNode is null)
        {
            return null;
        }

        var header = mainNode.SelectSingleNode(".//h1");

        var paragraphs = mainNode.SelectNodes(".//p").ToList();
        if (paragraphs.Count < 2)
        {
            return null;
        }

        var title = header.InnerText;
        var description = paragraphs[1].InnerText;

        return new Rule(
            code,
            title,
            link,
            category: null,
            description);
    }
}