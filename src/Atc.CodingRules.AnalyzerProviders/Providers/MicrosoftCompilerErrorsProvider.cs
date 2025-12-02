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

    public override Uri? DocumentationLink { get; set; } = new("https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/", UriKind.Absolute);

    protected override AnalyzerProviderBaseRuleData CreateData()
        => new(Name);

    protected override async Task ReCollect(AnalyzerProviderBaseRuleData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var categoryPages = new[]
        {
            "compiler-messages/preprocessor-errors",
            "compiler-messages/attribute-usage-errors",
            "compiler-messages/feature-version-errors",
            "compiler-messages/assembly-references",
            "compiler-messages/constructor-errors",
            "compiler-messages/overloaded-operator-errors",
            "compiler-messages/parameter-argument-mismatch",
            "compiler-messages/generic-type-parameters-errors",
            "compiler-messages/async-await-errors",
            "compiler-messages/interface-implementation-errors",
            "compiler-messages/ref-modifiers-errors",
            "compiler-messages/ref-safety-errors",
            "compiler-messages/ref-struct-errors",
            "compiler-messages/iterator-yield",
            "compiler-messages/extension-declarations",
            "compiler-messages/partial-declarations",
            "compiler-messages/params-arrays",
            "compiler-messages/nullable-warnings",
            "compiler-messages/pattern-matching-warnings",
            "compiler-messages/string-literal",
            "compiler-messages/array-declaration-errors",
            "compiler-messages/inline-array-errors",
            "compiler-messages/lambda-expression-errors",
            "compiler-messages/overload-resolution",
            "compiler-messages/expression-tree-restrictions",
            "compiler-messages/using-directive-errors",
            "compiler-messages/using-statement-declaration-errors",
            "compiler-messages/source-generator-errors",
            "compiler-messages/static-abstract-interfaces",
            "compiler-messages/lock-semantics",
            "compiler-messages/dynamic-type-and-binding-errors",
            "compiler-messages/unsafe-code-errors",
            "compiler-messages/warning-waves",
        };

        foreach (var categoryPath in categoryPages)
        {
            var categoryUri = new Uri(DocumentationLink!, categoryPath);
            var rules = await GetRulesFromCategoryPage(categoryUri.AbsoluteUri);
            foreach (var rule in rules)
            {
                data.Rules.Add(rule);
            }
        }
    }

    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "OK.")]
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "OK - graceful degradation for individual category pages.")]
    private static async Task<List<Rule>> GetRulesFromCategoryPage(
        string categoryUrl)
    {
        var rules = new List<Rule>();
        var web = new HtmlWeb();

        try
        {
            var htmlDoc = await web
                .LoadFromWebAsync(categoryUrl)
                .ConfigureAwait(false);

            if (htmlDoc.DocumentNode.HasTitleWithAccessDenied())
            {
                return rules;
            }

            var mainNode = htmlDoc.DocumentNode.SelectSingleNode("//main[@id='main']");
            if (mainNode is null)
            {
                return rules;
            }

            // Find all strong elements that contain CS error codes
            var strongNodes = mainNode.SelectNodes(".//strong");
            if (strongNodes is null)
            {
                return rules;
            }

            foreach (var strongNode in strongNodes)
            {
                var text = strongNode.InnerText.Trim();
                if (!text.StartsWith("CS", StringComparison.Ordinal))
                {
                    continue;
                }

                // Extract just the CS code (e.g., "CS1024" from "CS1024:")
                var codeTrimmed = text.TrimEnd(':');
                var code = codeTrimmed.Trim();
                if (code.Length is < 5 or > 7)
                {
                    continue;
                }

                // Get the description from the following text
                var description = string.Empty;
                var nextSibling = strongNode.NextSibling;
                if (nextSibling is not null)
                {
                    var deEntitized = HtmlEntity.DeEntitize(nextSibling.InnerText);
                    var trimmed = deEntitized.Trim();
                    var withoutColon = trimmed.TrimStart(':');
                    description = withoutColon.Trim();
                }

                var link = categoryUrl + "#" + code.ToLowerInvariant();

                rules.Add(
                    new Rule(
                        code,
                        code,
                        link,
                        category: null,
                        description));
            }
        }
        catch
        {
            // Ignore errors for individual category pages
        }

        return rules;
    }
}