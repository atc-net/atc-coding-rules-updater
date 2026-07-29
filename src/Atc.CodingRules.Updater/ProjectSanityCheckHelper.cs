namespace Atc.CodingRules.Updater;

/// <summary>
/// Pre-flight validation run before <see cref="EditorConfigHelper"/> / <see cref="DirectoryBuildPropsHelper"/>
/// touch any file. Catches misconfigurations that would otherwise produce broken builds (missing
/// <c>OrganizationName</c>, redundant <c>EnableNETAnalyzers</c>, <c>ImplicitUsings</c> against an
/// older <c>TargetFramework</c>, …).
/// </summary>
public static class ProjectSanityCheckHelper
{
    /// <summary>
    /// Validates the props/csproj layout under <paramref name="projectPath"/> and returns every
    /// diagnostic found, without logging or throwing. Used by the JSON output mode and as the
    /// implementation behind <see cref="CheckFiles"/>.
    /// </summary>
    /// <param name="projectPath">Project root directory.</param>
    /// <param name="projectTarget">Target framework profile chosen for this run.</param>
    /// <returns>Read-only list of diagnostics; empty when the project is clean.</returns>
    public static IReadOnlyList<SanityCheckDiagnostic> CheckFilesAndCollect(
        DirectoryInfo projectPath,
        SupportedProjectTargetType projectTarget)
    {
        var diagnostics = new List<SanityCheckDiagnostic>();

        CheckMissingOrganizationName(diagnostics, projectPath);
        CheckMissingRepositoryName(diagnostics, projectPath);

        switch (projectTarget)
        {
            case SupportedProjectTargetType.DotNet5:
                CheckEnableNetAnalyzers(diagnostics, projectPath, projectTarget);
                CheckTargetFrameworkAndImplicitUsings(diagnostics, projectPath, "netcoreapp3.1");
                break;
            case SupportedProjectTargetType.DotNet6:
            case SupportedProjectTargetType.DotNet7:
            case SupportedProjectTargetType.DotNet8:
            case SupportedProjectTargetType.DotNet9:
            case SupportedProjectTargetType.DotNet10:
            case SupportedProjectTargetType.DotNet11:
                CheckTargetFrameworkAndImplicitUsings(diagnostics, projectPath, "netcoreapp3.1");
                break;
        }

        return diagnostics;
    }

    /// <summary>
    /// Validates the props/csproj layout under <paramref name="projectPath"/> against the chosen
    /// <paramref name="projectTarget"/> and reports the result via <paramref name="logger"/>.
    /// </summary>
    /// <param name="throwIf">When <c>true</c>, hard violations throw <see cref="DataException"/>; when <c>false</c>, every diagnostic is logged as a warning instead.</param>
    /// <param name="logger">Where warnings are reported.</param>
    /// <param name="projectPath">Project root directory.</param>
    /// <param name="projectTarget">Target framework profile chosen for this run.</param>
    public static IReadOnlyList<SanityCheckDiagnostic> CheckFiles(
        bool throwIf,
        ILogger logger,
        DirectoryInfo projectPath,
        SupportedProjectTargetType projectTarget)
    {
        var diagnostics = CheckFilesAndCollect(projectPath, projectTarget);
        if (diagnostics.Count == 0)
        {
            return diagnostics;
        }

        // Errors are reported in groups by code so the output matches the historical wording
        // (one header line + indented file paths) and the throwIf path can produce one
        // DataException per code group.
        var errorGroups = diagnostics
            .Where(d => d.Severity == SanityCheckSeverity.Error)
            .GroupBy(d => d.Code, StringComparer.Ordinal)
            .ToList();

        foreach (var group in errorGroups)
        {
            var header = group.First().Message;
            if (throwIf)
            {
                var sb = new StringBuilder();
                sb.AppendLine(header);
                foreach (var diagnostic in group.Where(d => d.FilePath is not null))
                {
                    sb.AppendLine(5, diagnostic.FilePath!);
                }

                throw new DataException(sb.ToString());
            }

            logger.LogWarning(header);
            foreach (var diagnostic in group.Where(d => d.FilePath is not null))
            {
                logger.LogWarning($"     {diagnostic.FilePath}");
            }
        }

        foreach (var diagnostic in diagnostics.Where(d => d.Severity == SanityCheckSeverity.Warning))
        {
            logger.LogWarning(diagnostic.Message);
        }

        return diagnostics;
    }

    private static void CheckMissingOrganizationName(
        ICollection<SanityCheckDiagnostic> diagnostics,
        DirectoryInfo projectPath)
    {
        var foundFiles = DirectoryBuildPropsHelper.SearchAllForElement(
            projectPath,
            "OrganizationName",
            "<!-- insert organization name here -->",
            SearchOption.TopDirectoryOnly);

        if (foundFiles.Count == 0)
        {
            return;
        }

        diagnostics.Add(new SanityCheckDiagnostic(
            SanityCheckSeverity.Warning,
            code: "MissingOrganizationName",
            message: $"OrganizationName in /{DirectoryBuildPropsHelper.FileName} is not set yet, please fix"));
    }

    private static void CheckMissingRepositoryName(
        ICollection<SanityCheckDiagnostic> diagnostics,
        DirectoryInfo projectPath)
    {
        var foundFiles = DirectoryBuildPropsHelper.SearchAllForElement(
            projectPath,
            "RepositoryName",
            "<!-- insert repository name here -->",
            SearchOption.TopDirectoryOnly);

        if (foundFiles.Count == 0)
        {
            return;
        }

        diagnostics.Add(new SanityCheckDiagnostic(
            SanityCheckSeverity.Warning,
            code: "MissingRepositoryName",
            message: $"RepositoryName in /{DirectoryBuildPropsHelper.FileName} is not set yet, please fix"));
    }

    private static void CheckEnableNetAnalyzers(
        ICollection<SanityCheckDiagnostic> diagnostics,
        DirectoryInfo projectPath,
        SupportedProjectTargetType projectTarget)
    {
        var foundFiles = DotnetCsProjHelper.SearchAllForElement(
            projectPath,
            "EnableNETAnalyzers",
            "true",
            SearchOption.AllDirectories,
            StringComparison.OrdinalIgnoreCase);

        if (foundFiles.Count == 0)
        {
            return;
        }

        var header = $"EnableNETAnalyzers in .csproj causes build errors when /Directory.Build.Props has projectTarget '{projectTarget}', please remove the element from the following files:";
        foreach (var file in foundFiles)
        {
            diagnostics.Add(new SanityCheckDiagnostic(
                SanityCheckSeverity.Error,
                code: "EnableNETAnalyzers",
                message: header,
                filePath: file.FullName));
        }
    }

    private static void CheckTargetFrameworkAndImplicitUsings(
        ICollection<SanityCheckDiagnostic> diagnostics,
        DirectoryInfo projectPath,
        string targetFramework)
    {
        var foundDirectoryBuildPropsFilesWithImplicitUsings = DirectoryBuildPropsHelper.SearchAllForElement(
            projectPath,
            "ImplicitUsings",
            "enable",
            SearchOption.TopDirectoryOnly,
            StringComparison.OrdinalIgnoreCase);

        if (foundDirectoryBuildPropsFilesWithImplicitUsings.Count == 0)
        {
            return;
        }

        var foundFiles = DotnetCsProjHelper.SearchAllForElement(
            projectPath,
            "TargetFramework",
            targetFramework);

        if (foundFiles.Count == 0)
        {
            return;
        }

        var header = $"TargetFramework '{targetFramework}' in .csproj can causes build errors when /Directory.Build.Props has ImplicitUsings enabled, please manually upgrade the following files:";
        foreach (var file in foundFiles)
        {
            diagnostics.Add(new SanityCheckDiagnostic(
                SanityCheckSeverity.Error,
                code: "TargetFrameworkImplicitUsingsConflict",
                message: header,
                filePath: file.FullName));
        }
    }
}