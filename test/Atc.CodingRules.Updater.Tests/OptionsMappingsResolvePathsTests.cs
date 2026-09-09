namespace Atc.CodingRules.Updater.Tests;

/// <summary>
/// Mapping paths in the options file are relative to the project, so they have to be anchored to
/// the project path — not to whatever directory the process happens to be started from.
/// </summary>
/// <remarks>
/// <para>
/// These cases are deterministic only because the anchoring no longer depends on the current
/// directory. That is the whole point: running the tool from outside the project silently wrote
/// into the wrong tree, and nothing failed loudly enough to notice.
/// </para>
/// <para>
/// The project path is built from the temp directory rather than a literal such as
/// <c>D:\Code\MyRepo</c>, because that literal is not rooted on Linux or macOS — it would be
/// anchored as a relative path there, and the expectations would drift per platform.
/// </para>
/// </remarks>
public sealed class OptionsMappingsResolvePathsTests
{
    private static readonly DirectoryInfo ProjectPath =
        new(Path.GetFullPath(Path.Combine(Path.GetTempPath(), "MyRepo")));

    [Theory]
    [InlineData("sample")]
    [InlineData("./sample")]
    [InlineData(".\\sample")]
    [InlineData("\\sample")]
    public void ResolvePaths_AnchorsRelativePathToProjectPath(
        string configuredPath)
    {
        var sut = new OptionsMappings();
        sut.Sample.Paths.Add(configuredPath);

        sut.ResolvePaths(ProjectPath);

        sut.Sample.Paths[0]
            .Should().Be(Path.Combine(ProjectPath.FullName, "sample"));
    }

    /// <summary>
    /// An options file is checked in and shared, so a nested path written with either separator
    /// has to resolve the same way whichever platform the tool runs on.
    /// </summary>
    /// <param name="configuredPath">The nested path as written in the options file.</param>
    [Theory]
    [InlineData("src/nested")]
    [InlineData("src\\nested")]
    public void ResolvePaths_NormalizesSeparatorsInNestedPath(
        string configuredPath)
    {
        var sut = new OptionsMappings();
        sut.Src.Paths.Add(configuredPath);

        sut.ResolvePaths(ProjectPath);

        sut.Src.Paths[0]
            .Should().Be(Path.Combine(ProjectPath.FullName, "src", "nested"));
    }

    /// <summary>
    /// On Windows a leading forward slash is drive-relative, not fully qualified, so it means
    /// "from the project root" and has to be anchored like any other relative path.
    /// </summary>
    /// <remarks>
    /// Windows only: on Linux and macOS the same string is an absolute path from the filesystem
    /// root, which this method correctly leaves alone instead.
    /// </remarks>
    [Fact]
    public void ResolvePaths_AnchorsDriveRelativePathToProjectPath()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "A leading '/' is absolute outside Windows.");

        var sut = new OptionsMappings();
        sut.Sample.Paths.Add("/sample");

        sut.ResolvePaths(ProjectPath);

        sut.Sample.Paths[0]
            .Should().Be(Path.Combine(ProjectPath.FullName, "sample"));
    }

    [Fact]
    public void ResolvePaths_LeavesFullyQualifiedPathAlone()
    {
        var absolute = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "Elsewhere", "test"));

        var sut = new OptionsMappings();
        sut.Test.Paths.Add(absolute);

        sut.ResolvePaths(ProjectPath);

        sut.Test.Paths[0].Should().Be(absolute);
    }

    /// <summary>
    /// A UNC path is fully qualified even though it starts with a separator, so it must not be
    /// mistaken for a project-relative path and combined onto the project path.
    /// </summary>
    /// <remarks>
    /// Windows only: a UNC share is a Windows concept. On Linux and macOS the same string is an
    /// ordinary relative path, which this method is supposed to anchor rather than leave alone.
    /// </remarks>
    [Fact]
    public void ResolvePaths_LeavesUncPathAlone()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "UNC paths only exist on Windows.");

        var unc = new string(Path.DirectorySeparatorChar, 2) + Path.Combine("server", "share", "src");

        var sut = new OptionsMappings();
        sut.Src.Paths.Add(unc);

        sut.ResolvePaths(ProjectPath);

        sut.Src.Paths[0].Should().Be(unc);
    }
}