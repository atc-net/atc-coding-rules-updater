namespace Atc.CodingRules.AnalyzerProviders.Providers;

public class AsyncifyProvider : AnalyzerProviderBase
{
    [SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification = "OK.")]
    private const string GitRawAnalyzerProviderBaseRulesBasePath = "https://raw.githubusercontent.com/hvanbakel/Asyncify-CSharp/master/Asyncify/Asyncify/Resources.resx";

    public AsyncifyProvider(ILogger logger, bool logWithAnsiConsoleMarkup = false)
        : base(logger, logWithAnsiConsoleMarkup)
    {
    }

    public static string Name => "Asyncify";

    public override Uri? DocumentationLink { get; set; } = new ("https://github.com/hvanbakel/Asyncify-CSharp", UriKind.Absolute);

    protected override AnalyzerProviderBaseRuleData CreateData()
        => new (Name);

    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "OK.")]
    protected override async Task ReCollect(
        AnalyzerProviderBaseRuleData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var web = new HtmlWeb();
        var htmlDoc = await web.LoadFromWebAsync(GitRawAnalyzerProviderBaseRulesBasePath).ConfigureAwait(false);

        var xml = new XmlDocument();
        xml.LoadXml(htmlDoc.Text);

        var dataNodes = xml.SelectNodes("/root/data");
        if (dataNodes is null)
        {
            return;
        }

        var codes = new List<string>();
        var titles = new List<Tuple<string, string>>();
        var descriptions = new List<Tuple<string, string>>();
        foreach (var dataNode in dataNodes)
        {
            if (dataNode is not XmlElement xmlElement)
            {
                continue;
            }

            if (xmlElement.Attributes.Count == 0)
            {
                continue;
            }

            var nameAttribute = xmlElement.Attributes["name"];
            if (nameAttribute is null ||
                !nameAttribute.Value.StartsWith("Asyncify", StringComparison.Ordinal))
            {
                continue;
            }

            var code = nameAttribute.Value;

            if (nameAttribute.Value.EndsWith("Title", StringComparison.Ordinal))
            {
                code = code.Replace("Title", string.Empty, StringComparison.Ordinal);
                var title = code.Replace("Asyncify", string.Empty, StringComparison.Ordinal).NormalizePascalCase();
                titles.Add(Tuple.Create(code, title));
            }
            else if (nameAttribute.Value.EndsWith("Description", StringComparison.Ordinal))
            {
                code = code.Replace("Description", string.Empty, StringComparison.Ordinal);
            }
            else if (nameAttribute.Value.EndsWith("MessageFormat", StringComparison.Ordinal))
            {
                code = code.Replace("MessageFormat", string.Empty, StringComparison.Ordinal);
                var description = xmlElement.InnerText.Replace("\n", string.Empty, StringComparison.Ordinal).Trim();
                descriptions.Add(Tuple.Create(code, description));
            }

            if (!codes.Contains(code, StringComparer.Ordinal))
            {
                codes.Add(code);
            }
        }

        foreach (var code in codes)
        {
            var title = titles.First(x => x.Item1.Equals(code, StringComparison.Ordinal))?.Item2;
            if (title is null)
            {
                continue;
            }

            var description = descriptions.First(x => x.Item1.Equals(code, StringComparison.Ordinal))?.Item2;
            var link = this.DocumentationLink!.AbsoluteUri;

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