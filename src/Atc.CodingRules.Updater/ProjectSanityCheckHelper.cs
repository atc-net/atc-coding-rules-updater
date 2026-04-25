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
    /// Validates the props/csproj layout under <paramref name="projectPath"/> against the chosen
    /// <paramref name="projectTarget"/>.
    /// </summary>
    /// <param name="throwIf">When <c>true</c>, hard violations throw <see cref="DataException"/>; when <c>false</c>, every diagnostic is logged as a warning instead.</param>
    /// <param name="logger">Where warnings are reported.</param>
    /// <param name="projectPath">Project root directory.</param>
    /// <param name="projectTarget">Target framework profile chosen for this run.</param>
    public static void CheckFiles(
        bool throwIf,
        ILogger logger,
        DirectoryInfo projectPath,
        SupportedProjectTargetType projectTarget)
    {
        MissingOrganizationName(logger, projectPath);
        MissingRepositoryName(logger, projectPath);

        switch (projectTarget)
        {
            case SupportedProjectTargetType.DotNet5:
                HasEnableNetAnalyzers(throwIf, logger, projectPath, projectTarget);
                HasTargetFrameworkAndImplicitUsings(throwIf, logger, projectPath, "netcoreapp3.1");
                break;
            case SupportedProjectTargetType.DotNet6:
            case SupportedProjectTargetType.DotNet7:
            case SupportedProjectTargetType.DotNet8:
            case SupportedProjectTargetType.DotNet9:
            case SupportedProjectTargetType.DotNet10:
            case SupportedProjectTargetType.DotNet11:
                HasTargetFrameworkAndImplicitUsings(throwIf, logger, projectPath, "netcoreapp3.1");
                break;
        }
    }

    private static void MissingOrganizationName(
        ILogger logger,
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

        logger.LogWarning($"OrganizationName in /{DirectoryBuildPropsHelper.FileName} is not set yet, please fix");
    }

    private static void MissingRepositoryName(
        ILogger logger,
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

        logger.LogWarning($"RepositoryName in /{DirectoryBuildPropsHelper.FileName} is not set yet, please fix");
    }

    private static void HasEnableNetAnalyzers(
        bool throwIf,
        ILogger logger,
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

        var sb = new StringBuilder();
        var header = $"EnableNETAnalyzers in .csproj causes build errors when /Directory.Build.Props has projectTarget '{projectTarget}', please remove the element from the following files:";
        if (throwIf)
        {
            sb.AppendLine(header);
        }
        else
        {
            logger.LogWarning(header);
        }

        foreach (var fileFullName in foundFiles.Select(x => x.FullName))
        {
            if (throwIf)
            {
                sb.AppendLine(5, fileFullName);
            }
            else
            {
                logger.LogWarning($"     {fileFullName}");
            }
        }

        if (throwIf)
        {
            throw new DataException(sb.ToString());
        }
    }

    private static void HasTargetFrameworkAndImplicitUsings(
        bool throwIf,
        ILogger logger,
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

        var sb = new StringBuilder();
        var header = $"TargetFramework '{targetFramework}' in .csproj can causes build errors when /Directory.Build.Props has ImplicitUsings enabled, please manually upgrade the following files:";
        if (throwIf)
        {
            sb.AppendLine(header);
        }
        else
        {
            logger.LogWarning(header);
        }

        foreach (var fileFullName in foundFiles.Select(x => x.FullName))
        {
            if (throwIf)
            {
                sb.AppendLine(5, fileFullName);
            }
            else
            {
                logger.LogWarning($"     {fileFullName}");
            }
        }

        if (throwIf)
        {
            throw new DataException(sb.ToString());
        }
    }
}