namespace Atc.CodingRules.Updater.Tests;

/// <summary>
/// A throw-away directory tree under the temp folder, deleted on dispose.
/// </summary>
internal sealed class TestWorkspace : IDisposable
{
    public TestWorkspace()
    {
        Root = new DirectoryInfo(Path.Combine(
            Path.GetTempPath(),
            "atc-coding-rules-updater-tests",
            Guid.NewGuid().ToString("N")));
        Root.Create();
    }

    public DirectoryInfo Root { get; }

    public FileInfo WriteFile(
        string relativePath,
        string content)
    {
        var file = new FileInfo(Path.Combine(Root.FullName, relativePath));
        file.Directory!.Create();
        File.WriteAllText(file.FullName, content);
        return file;
    }

    public string ReadFile(string relativePath)
        => File.ReadAllText(Path.Combine(Root.FullName, relativePath));

    public bool FileExists(string relativePath)
        => File.Exists(Path.Combine(Root.FullName, relativePath));

    public DirectoryInfo Directory(string relativePath)
        => new(Path.Combine(Root.FullName, relativePath));

    public void Dispose()
    {
        try
        {
            Root.Delete(recursive: true);
        }
        catch (IOException)
        {
            // Best effort: a locked file must not fail the test.
        }
        catch (UnauthorizedAccessException)
        {
            // Best effort: a locked file must not fail the test.
        }
    }
}