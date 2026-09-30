namespace Atc.CodingRules.Updater.CentralPackageManagement.Migration;

/// <summary>
/// The real <see cref="ICpmMigrationEnvironment"/>: runs <c>git</c> and <c>dotnet restore</c>.
/// </summary>
public sealed class DotnetCpmMigrationEnvironment : ICpmMigrationEnvironment
{
    private static readonly Regex RestoreWarning = new(
        @"warning (?<code>NU\d{4}): (?<message>.*?)(?: \[|$)",
        RegexOptions.Multiline | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    /// <inheritdoc />
    public async Task<bool?> IsWorkingTreeCleanAsync(
        DirectoryInfo projectPath,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(projectPath);

        var (exitCode, output) = await RunAsync("git", projectPath, ["status", "--porcelain"], cancellationToken);
        if (exitCode != 0)
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(output);
    }

    /// <inheritdoc />
    public async Task<RestoreSnapshot> RestoreAsync(
        DirectoryInfo projectPath,
        string? restoreTarget,
        string configuration,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(projectPath);

        var arguments = new List<string> { "restore" };
        if (!string.IsNullOrEmpty(restoreTarget))
        {
            arguments.Add(restoreTarget);
        }

        // --force re-resolves every project, so the assets files describe this restore and not
        // an earlier one that happened to be up to date.
        arguments.Add("--force");
        arguments.Add($"-p:Configuration={configuration}");

        // File timestamps can be coarse, so allow a little slack before the restore started.
        var restoreStarted = DateTime.UtcNow.AddSeconds(-2);
        var (exitCode, output) = await RunAsync("dotnet", projectPath, arguments, cancellationToken);

        var warnings = RestoreWarning
            .Matches(output)
            .Select(x => $"{x.Groups["code"].Value}: {x.Groups["message"].Value.Trim()}")
            .ToHashSet(StringComparer.Ordinal);

        return new RestoreSnapshot(exitCode, warnings, ReadAssets(projectPath, restoreStarted));
    }

    /// <summary>
    /// Reads the resolved packages from every <c>project.assets.json</c> under
    /// <paramref name="projectPath"/> written since <paramref name="writtenSince"/>, keyed by its
    /// path relative to the root with <c>/</c> separators. Older files are left out: they come
    /// from an earlier build, not from this restore, and would hide a project it never covered.
    /// </summary>
    internal static Dictionary<string, IReadOnlySet<string>> ReadAssets(
        DirectoryInfo projectPath,
        DateTime writtenSince)
    {
        var result = new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal);
        var files = Directory
            .EnumerateFiles(projectPath.FullName, "project.assets.json", SearchOption.AllDirectories)
            .Where(x => File.GetLastWriteTimeUtc(x) >= writtenSince);

        foreach (var file in files)
        {
            var key = Path.GetRelativePath(projectPath.FullName, file).Replace('\\', '/');
            var packages = new HashSet<string>(StringComparer.Ordinal);

            using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(file));
            if (document.RootElement.TryGetProperty("targets", out var targets))
            {
                foreach (var target in targets.EnumerateObject())
                {
                    foreach (var library in target.Value.EnumerateObject())
                    {
                        if (library.Value.TryGetProperty("type", out var type) &&
                            type.GetString() == "package")
                        {
                            packages.Add($"{target.Name}|{library.Name}");
                        }
                    }
                }
            }

            result[key] = packages;
        }

        return result;
    }

    private static async Task<(int ExitCode, string Output)> RunAsync(
        string fileName,
        DirectoryInfo workingDirectory,
        IEnumerable<string> arguments,
        CancellationToken cancellationToken)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo(fileName)
        {
            WorkingDirectory = workingDirectory.FullName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = System.Diagnostics.Process.Start(startInfo)
                            ?? throw new InvalidOperationException($"Could not start {fileName}.");

        var standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardError = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        return (process.ExitCode, await standardOutput + await standardError);
    }
}