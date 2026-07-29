namespace Atc.CodingRules.Updater.Tests;

/// <summary>
/// Covers the <c>--buildProperty</c> / <c>--buildConfiguration</c> plumbing added for issue #118.
/// </summary>
/// <remarks>
/// MSBuild has no switch to skip a named target, so the updater cannot solve that problem on its
/// own. What it can do is forward properties, letting a conditioned target be turned off from the
/// command line.
/// </remarks>
public sealed class BuildArgumentsTests
{
    [Fact]
    public void BuildAdditionalArguments_ReturnsEmpty_WhenNoPropertiesGiven()
    {
        var actual = ProjectHelper.BuildAdditionalArguments([]);

        actual.Should().BeEmpty();
    }

    [Fact]
    public void BuildAdditionalArguments_FormatsOnePropertyAsMsBuildSwitch()
    {
        var actual = ProjectHelper.BuildAdditionalArguments(["SkipObfuscation=true"]);

        actual.Should().Be("-p:SkipObfuscation=true");
    }

    [Fact]
    public void BuildAdditionalArguments_FormatsSeveralPropertiesSpaceSeparated()
    {
        var actual = ProjectHelper.BuildAdditionalArguments(["SkipObfuscation=true", "SkipSigning=true"]);

        actual.Should().Be("-p:SkipObfuscation=true -p:SkipSigning=true");
    }

    [Fact]
    public void BuildAdditionalArguments_QuotesValuesContainingSpaces()
    {
        var actual = ProjectHelper.BuildAdditionalArguments(["OutputPath=C:\\my builds\\out"]);

        actual.Should().Be("-p:\"OutputPath=C:\\my builds\\out\"");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("NoEqualsSign")]
    public void BuildAdditionalArguments_SkipsEntriesThatAreNotNameValuePairs(
        string property)
    {
        var actual = ProjectHelper.BuildAdditionalArguments([property]);

        actual.Should().BeEmpty();
    }

    [Fact]
    public void BuildAdditionalArguments_KeepsValuesContainingAnEqualsSign()
    {
        // A base64 or connection-string value can legitimately contain '='.
        var actual = ProjectHelper.BuildAdditionalArguments(["Token=abc=="]);

        actual.Should().Be("-p:Token=abc==");
    }

    [Theory]
    [InlineData("Release", true)]
    [InlineData("release", true)]
    [InlineData("Debug", false)]
    [InlineData("debug", false)]
    [InlineData("", true)]
    public void IsReleaseConfiguration_MapsTheConfigurationName(
        string configuration,
        bool expected)
    {
        // Release stays the default, so existing behaviour is preserved when nothing is passed.
        ProjectHelper.IsReleaseConfiguration(configuration).Should().Be(expected);
    }
}