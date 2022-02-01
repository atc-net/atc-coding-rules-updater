using System.Diagnostics;
using System.Reflection;

namespace Atc.CodingRules.Updater;

public static class CodingRulesUpdaterVersionHelper
{
    public static Version GetCurrentVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var fileVersion = FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion;
        if (fileVersion is null)
        {
            return new Version(1, 0, 0, 0);
        }

        return Version.TryParse(fileVersion, out var version)
            ? version
            : new Version(1, 0, 0, 0);
    }

    public static Version? GetLatestVersion()
        => AtcApiNugetClientHelper.GetLatestVersionForPackageId("atc-coding-rules-updater");

    public static bool IsLatestVersion()
    {
        var currentVersion = GetCurrentVersion();
        if (currentVersion == new Version(1, 0, 0, 0))
        {
            return true;
        }

        var latestVersion = GetLatestVersion();
        return latestVersion is null || !latestVersion.GreaterThan(currentVersion);
    }

    public static void PrintUpdateInfoIfNeeded(
        ILogger logger)
    {
        if (IsLatestVersion())
        {
            return;
        }

        var currentVersion = GetCurrentVersion();
        var latestVersion = GetLatestVersion()!;
        logger.LogWarning($"Version {latestVersion} of ATC-Coding-Rules-Updater is available!");
        logger.LogWarning($"You are running version {currentVersion}");
        logger.LogWarning(string.Empty);
        logger.LogWarning("To update run the following command:");
        logger.LogWarning("   dotnet tool update --global atc-coding-rules-updater");
        logger.LogWarning(string.Empty);
    }
}