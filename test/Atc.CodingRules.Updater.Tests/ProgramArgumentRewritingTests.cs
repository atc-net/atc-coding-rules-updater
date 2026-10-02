namespace Atc.CodingRules.Updater.Tests;

/// <summary>
/// Characterization tests for the argument pre-processing in <see cref="Program"/>. This logic
/// rewrites shorthand invocations (<c>atc-coding-rules-updater .</c>) into the full command form
/// before Spectre parses them, and had no test coverage.
/// </summary>
public sealed class ProgramArgumentRewritingTests
{
    [Fact]
    public void SetProjectPathFromDotArgumentIfNeeded_LeavesArgumentsUntouched_WhenNoDotPresent()
    {
        string[] args = ["run", "-p", @"c:\temp\MyProject"];

        var actual = Program.SetProjectPathFromDotArgumentIfNeeded(args);

        actual.Should().Equal(args);
    }

    [Fact]
    public void SetProjectPathFromDotArgumentIfNeeded_ExpandsBareDot_ToRunWithProjectPathAndVerbose()
    {
        string[] args = ["."];

        var actual = Program.SetProjectPathFromDotArgumentIfNeeded(args);

        actual.Should().Equal("run", "-p", Environment.CurrentDirectory, "--verbose");
    }

    [Fact]
    public void SetProjectPathFromDotArgumentIfNeeded_DoesNotInsertRun_WhenCommandAlreadyGiven()
    {
        string[] args = ["sanity-check", "."];

        var actual = Program.SetProjectPathFromDotArgumentIfNeeded(args);

        actual.Should().Equal("sanity-check", "-p", Environment.CurrentDirectory, "--verbose");
    }

    [Fact]
    public void SetProjectPathFromDotArgumentIfNeeded_DoesNotDuplicateProjectPathFlag_WhenAlreadyPresent()
    {
        string[] args = ["run", "-p", "."];

        var actual = Program.SetProjectPathFromDotArgumentIfNeeded(args);

        actual.Should().Equal("run", "-p", Environment.CurrentDirectory, "--verbose");
    }

    [Theory]
    [InlineData("run")]
    [InlineData("sanity-check")]
    public void SetProjectPathFromDotArgumentIfNeeded_KeepsTheGivenCommand_ForBareDot(
        string command)
    {
        var actual = Program.SetProjectPathFromDotArgumentIfNeeded([command, "."]);

        actual.Should().Equal(command, "-p", Environment.CurrentDirectory, "--verbose");
    }

    [Theory]
    [InlineData("run")]
    [InlineData("sanity-check")]
    public void SetProjectPathFromDotArgumentIfNeeded_KeepsTheGivenCommand_ForProjectPathDot(
        string command)
    {
        var actual = Program.SetProjectPathFromDotArgumentIfNeeded([command, "-p", ".", "--dry-run"]);

        actual.Should().Equal(command, "-p", Environment.CurrentDirectory, "--dry-run", "--verbose");
    }

    [Theory]
    [InlineData("validate")]
    [InlineData("migrate")]
    public void SetProjectPathFromDotArgumentIfNeeded_KeepsTheCpmBranchCommand_ForBareDot(
        string verb)
    {
        var actual = Program.SetProjectPathFromDotArgumentIfNeeded(["cpm", verb, "."]);

        actual.Should().Equal("cpm", verb, "-p", Environment.CurrentDirectory, "--verbose");
    }

    [Theory]
    [InlineData("validate")]
    [InlineData("migrate")]
    public void SetProjectPathFromDotArgumentIfNeeded_KeepsTheCpmBranchCommand_ForProjectPathDot(
        string verb)
    {
        var actual = Program.SetProjectPathFromDotArgumentIfNeeded(["cpm", verb, "-p", ".", "--dry-run"]);

        actual.Should().Equal("cpm", verb, "-p", Environment.CurrentDirectory, "--dry-run", "--verbose");
    }

    /// <summary>
    /// Every camelCase long option name that has been released, and the kebab-case name it now
    /// maps to. Existing scripts use these spellings, so each one must keep working; removing an
    /// entry from <see cref="Program.LegacyOptionNames"/> fails this list.
    /// </summary>
    public static TheoryData<string, string> ReleasedOptionNames => new()
    {
        { "--projectPath", "--project-path" },
        { "--optionsPath", "--options-path" },
        { "--projectTarget", "--project-target" },
        { "--useLatestMinorNugetVersion", "--use-latest-minor-nuget-version" },
        { "--useTemporarySuppressions", "--use-temporary-suppressions" },
        { "--temporarySuppressionPath", "--temporary-suppression-path" },
        { "--temporarySuppressionAsExcel", "--temporary-suppression-as-excel" },
        { "--buildFile", "--build-file" },
        { "--buildConfiguration", "--build-configuration" },
        { "--buildProperty", "--build-property" },
        { "--fetchMode", "--fetch-mode" },
        { "--organizationName", "--organization-name" },
        { "--repositoryName", "--repository-name" },
        { "--forceNugetRefresh", "--force-nuget-refresh" },
        { "--failOnChanges", "--fail-on-changes" },
        { "--includeProviders", "--include-providers" },
        { "--excludeProviders", "--exclude-providers" },
    };

    [Theory]
    [MemberData(nameof(ReleasedOptionNames))]
    public void NormalizeLegacyOptionNames_MapsReleasedCamelCaseName_ToKebabCase(
        string legacyName,
        string kebabName)
    {
        var actual = Program.NormalizeLegacyOptionNames(["run", legacyName, "value"]);

        actual.Should().Equal("run", kebabName, "value");
    }

    [Theory]
    [MemberData(nameof(ReleasedOptionNames))]
    public void NormalizeLegacyOptionNames_MapsInlineValueForms(
        string legacyName,
        string kebabName)
    {
        var actual = Program.NormalizeLegacyOptionNames([$"{legacyName}=a", $"{legacyName}:b"]);

        actual.Should().Equal($"{kebabName}=a", $"{kebabName}:b");
    }

    [Fact]
    public void NormalizeLegacyOptionNames_IgnoresLetterCase()
    {
        var actual = Program.NormalizeLegacyOptionNames(["--PROJECTPATH", "."]);

        actual.Should().Equal("--project-path", ".");
    }

    [Fact]
    public void NormalizeLegacyOptionNames_LeavesKebabNamesValuesAndCommandsAlone()
    {
        string[] args = ["cpm", "migrate", "--project-path", @"c:\repo", "--dry-run", "-p", "projectPath"];

        var actual = Program.NormalizeLegacyOptionNames(args);

        actual.Should().Equal(args);
    }

    [Fact]
    public void LegacyOptionNames_MapOnlyToOptionNamesTheCliDefines()
    {
        var definedLongNames = typeof(ArgumentCommandConstants)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(x => x.IsLiteral)
            .Select(x => (string)x.GetRawConstantValue()!)
            .Where(x => x.StartsWith("--", StringComparison.Ordinal))
            .ToList();

        Program.LegacyOptionNames.Values.Should().OnlyContain(x => definedLongNames.Contains(x));
    }

    /// <summary>
    /// The shorthand rewrite inserts <c>run</c> unless a known command is present. A command
    /// missing from that list is silently turned into <c>run &lt;command&gt;</c>, which is how the
    /// former <c>migrate-to-cpm -p .</c> broke. This guard fails as soon as a new top-level
    /// command constant is added without being recognised.
    /// </summary>
    [Fact]
    public void CommandNames_ContainsEveryTopLevelCommand()
    {
        // Sub-commands only ever appear after their branch name, which is itself recognised.
        string[] subCommands = ["collect", "cleanup-cache", "validate", "migrate"];

        var topLevelCommands = typeof(NameCommandConstants)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(x => x.IsLiteral)
            .Select(x => (string)x.GetRawConstantValue()!)
            .Except(subCommands, StringComparer.Ordinal)
            .Append(Atc.Console.Spectre.CommandConstants.NameOptionsFile);

        Program.CommandNames.Should().Contain(topLevelCommands);
    }

    [Fact]
    public void SetHelpArgumentIfNeeded_ReturnsHelp_WhenNoArgumentsGiven()
    {
        var actual = Program.SetHelpArgumentIfNeeded([]);

        actual.Should().Equal("-h");
    }

    [Fact]
    public void SetHelpArgumentIfNeeded_ReturnsCommandScopedHelp_WhenProjectPathMissing()
    {
        var actual = Program.SetHelpArgumentIfNeeded(["sanity-check"]);

        actual.Should().Equal("sanity-check", "-h");
    }

    [Fact]
    public void SetHelpArgumentIfNeeded_ReturnsBranchHelp_ForCpmCommandWithoutProjectPath()
    {
        var actual = Program.SetHelpArgumentIfNeeded(["cpm", "migrate"]);

        actual.Should().Equal("cpm", "migrate", "-h");
    }

    [Fact]
    public void SetHelpArgumentIfNeeded_ReturnsBranchHelp_ForCpmWithoutCommand()
    {
        var actual = Program.SetHelpArgumentIfNeeded(["cpm"]);

        actual.Should().Equal("cpm", "-h");
    }

    [Theory]
    [InlineData("cpm", "validate", "--help")]
    [InlineData("cpm", "migrate", "-h")]
    [InlineData("run", "--help", null)]
    public void SetHelpArgumentIfNeeded_LeavesExplicitHelpRequestsAlone(
        string first,
        string second,
        string? third)
    {
        string[] args = third is null ? [first, second] : [first, second, third];

        var actual = Program.SetHelpArgumentIfNeeded(args);

        actual.Should().Equal(args);
    }

    [Fact]
    public void SetHelpArgumentIfNeeded_LeavesCleanupCacheAlone_BecauseItNeedsNoProjectPath()
    {
        string[] args = ["analyzer-providers", "cleanup-cache"];

        var actual = Program.SetHelpArgumentIfNeeded(args);

        actual.Should().Equal(args);
    }

    [Fact]
    public void SetHelpArgumentIfNeeded_LeavesArgumentsUntouched_WhenProjectPathGiven()
    {
        string[] args = ["run", "-p", @"c:\temp\MyProject"];

        var actual = Program.SetHelpArgumentIfNeeded(args);

        actual.Should().Equal(args);
    }
}