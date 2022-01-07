namespace Atc.CodingRules.AnalyzerProviders.Providers;

public class MicrosoftCompilerErrorsProvider : AnalyzerProviderBase
{
    public static string Name => "Microsoft.CompilerErrors";

    public override Uri? DocumentationLink { get; set; } = new ("https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages", UriKind.Absolute);

    protected override AnalyzerProviderBaseRuleData CreateData()
        => new (Name);

    protected override async Task ReCollect(
        AnalyzerProviderBaseRuleData data)
    {
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

            var jsonChildItems = jsonElement.GetProperty("children").EnumerateArray();
            while (jsonChildItems.MoveNext())
            {
                var jsonChildElement = jsonChildItems.Current;
                var code = jsonChildElement.GetProperty("toc_title").ToString()!;
                var hrefPart = jsonChildElement.GetProperty("href").ToString()!;

                var link = hrefPart.StartsWith("../../misc/", StringComparison.Ordinal)
                    ? "https://docs.microsoft.com/en-us/dotnet/csharp/" + hrefPart.Replace("../../", string.Empty, StringComparison.Ordinal)
                    : DocumentationLink.AbsoluteUri + "/" + hrefPart;

                var rule = await GetRuleByCode(code, link);
                if (rule is not null)
                {
                    data.Rules.Add(rule);
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

        var paragraphs = mainNode.SelectNodes(".//p").ToList();
        if (paragraphs.Count < 3)
        {
            return null;
        }

        var title = paragraphs[2].InnerText;
        var description = string.Empty;
        if (paragraphs.Count > 3)
        {
            description = paragraphs[3].InnerText;
        }

        return new Rule(
            code,
            title,
            link,
            category: null,
            description);
    }
}