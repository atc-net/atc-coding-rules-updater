namespace Atc.CodingRules.Updater;

public static class ProjectSanityCheckHelper
{
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
                break;
            case SupportedProjectTargetType.DotNet6:
            case SupportedProjectTargetType.DotNet7:
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

        if (!foundFiles.Any())
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

        if (!foundFiles.Any())
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

        if (!foundFiles.Any())
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

        if (!foundDirectoryBuildPropsFilesWithImplicitUsings.Any())
        {
            return;
        }

        var foundFiles = DotnetCsProjHelper.SearchAllForElement(
            projectPath,
            "TargetFramework",
            targetFramework);

        if (!foundFiles.Any())
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