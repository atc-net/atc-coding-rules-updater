namespace Atc.CodingRules.Updater.Tests;

/// <summary>
/// Mapping paths in the options file are relative to the project, so they have to be anchored to
/// the project path — not to whatever directory the process happens to be started from.
/// </summary>
/// <remarks>
/// These cases are deterministic only because the anchoring no longer depends on the current
/// directory. That is the whole point: running the tool from outside the project silently wrote
/// into the wrong tree, and nothing failed loudly enough to notice.
/// </remarks>
public sealed class OptionsMappingsResolvePathsTests
{
    private static readonly string ProjectPath = Path.Combine("D:", "Code", "MyRepo");

    [Theory]
    [InlineData("sample")]
    [InlineData("./sample")]
    [InlineData(@"\sample")]
    public void ResolvePaths_AnchorsRelativePathToProjectPath(
        string configuredPath)
    {
        var sut = new OptionsMappings();
        sut.Sample.Paths.Add(configuredPath);

        sut.ResolvePaths(new DirectoryInfo(ProjectPath));

        sut.Sample.Paths[0]
            .Should().Be(Path.Combine(ProjectPath, "sample"));
    }

    [Fact]
    public void ResolvePaths_AnchorsNestedRelativePathToProjectPath()
    {
        var sut = new OptionsMappings();
        sut.Src.Paths.Add("src/nested");

        sut.ResolvePaths(new DirectoryInfo(ProjectPath));

        sut.Src.Paths[0]
            .Should().Be(Path.Combine(ProjectPath, "src", "nested"));
    }

    [Fact]
    public void ResolvePaths_LeavesFullyQualifiedPathAlone()
    {
        var absolute = Path.Combine("D:", "Elsewhere", "test");

        var sut = new OptionsMappings();
        sut.Test.Paths.Add(absolute);

        sut.ResolvePaths(new DirectoryInfo(ProjectPath));

        sut.Test.Paths[0].Should().Be(absolute);
    }

    /// <summary>
    /// A UNC path is fully qualified even though it starts with a separator, so it must not be
    /// mistaken for a project-relative path and combined onto the project path.
    /// </summary>
    [Fact]
    public void ResolvePaths_LeavesUncPathAlone()
    {
        var unc = new string(Path.DirectorySeparatorChar, 2) + Path.Combine("server", "share", "src");

        var sut = new OptionsMappings();
        sut.Src.Paths.Add(unc);

        sut.ResolvePaths(new DirectoryInfo(ProjectPath));

        sut.Src.Paths[0].Should().Be(unc);
    }
}