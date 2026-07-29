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