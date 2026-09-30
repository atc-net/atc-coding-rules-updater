namespace Atc.CodingRules.Updater.Tests;

/// <summary>
/// Reads fixture files from the test project's source folder.
/// </summary>
internal static class TestFixtures
{
    public static string Read(string relativePath)
        => File.ReadAllText(Path.Combine(GetTestProjectPath(), relativePath));

    public static string DistributionRootProps()
        => Read("TestFilesCpm/Distribution/dotnet10/root.Directory.Build.props.xml");

    public static string DistributionTestProps()
        => Read("TestFilesCpm/Distribution/dotnet10/test.Directory.Build.props.xml");

    private static string GetTestProjectPath()
    {
        var testAssemblyName = Assembly.GetExecutingAssembly().GetName().Name!;
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var testBasePath = baseDir.Split(testAssemblyName, StringSplitOptions.RemoveEmptyEntries)[0];
        return Path.Combine(testBasePath, testAssemblyName);
    }
}