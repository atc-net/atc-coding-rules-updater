namespace Atc.CodingRules.Updater.Tests;

/// <summary>
/// Runs the real <c>dotnet</c> CLI from integration tests.
/// </summary>
internal static class DotnetCli
{
    public static async Task<(int ExitCode, string Output)> Run(
        DirectoryInfo workingDirectory,
        params string[] arguments)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo("dotnet")
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

        using var process = System.Diagnostics.Process.Start(startInfo)!;
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return (process.ExitCode, await standardOutput + await standardError);
    }
}