namespace Atc.CodingRules.Updater.Tests;

public sealed class SuppressionLinesTests
{
    [Fact]
    public void GetSuppressionLines_EmitsOneLinePerCode_WhenTwoProvidersDocumentTheSameCode()
    {
        // The rule sets currently have no overlapping codes, but nothing enforces that. If a
        // code ever appears in two providers, the .editorconfig must not end up with the same
        // dotnet_diagnostic key twice.
        var providers = new List<AnalyzerProviderBaseRuleData>
        {
            CreateProvider("Provider.A", "CS1998"),
            CreateProvider("Provider.B", "CS1998"),
        };

        var buildResult = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["CS1998"] = 3,
        };

        var actual = ProjectHelper.GetSuppressionLines(providers, buildResult);

        actual.SelectMany(x => x.Item2)
            .Should().ContainSingle(x => x.Contains("dotnet_diagnostic.CS1998.severity", StringComparison.Ordinal));
    }

    [Fact]
    public void GetSuppressionLines_GroupsKnownCodesUnderTheirProvider()
    {
        var providers = new List<AnalyzerProviderBaseRuleData>
        {
            CreateProvider("Provider.A", "CA1303"),
        };

        var buildResult = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["CA1303"] = 1,
        };

        var actual = ProjectHelper.GetSuppressionLines(providers, buildResult);

        actual.Should().ContainSingle(x => x.Item1.Equals("Provider.A", StringComparison.Ordinal));
    }

    [Fact]
    public void GetSuppressionLines_PutsUnrecognizedCodesUnderUnknown()
    {
        var providers = new List<AnalyzerProviderBaseRuleData>
        {
            CreateProvider("Provider.A", "CA1303"),
        };

        var buildResult = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["ZZ9999"] = 2,
        };

        var actual = ProjectHelper.GetSuppressionLines(providers, buildResult);

        actual.Should().ContainSingle(x => x.Item1.Equals("Unknown", StringComparison.Ordinal));
        actual.SelectMany(x => x.Item2)
            .Should().ContainSingle(x => x.Contains("ZZ9999", StringComparison.Ordinal)
                                         && x.Contains("2 occurrences", StringComparison.Ordinal));
    }

    private static AnalyzerProviderBaseRuleData CreateProvider(
        string name,
        params string[] codes)
    {
        var data = new AnalyzerProviderBaseRuleData(name);
        foreach (var code in codes)
        {
            data.Rules.Add(new AnalyzerProviders.Models.Rule(code, $"Title for {code}", link: string.Empty));
        }

        return data;
    }
}