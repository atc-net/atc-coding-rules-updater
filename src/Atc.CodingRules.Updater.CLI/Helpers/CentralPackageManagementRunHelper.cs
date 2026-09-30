namespace Atc.CodingRules.Updater.CLI.Helpers;

/// <summary>
/// The <c>Directory.Build.props</c> step of <c>run</c> for a repository that uses Central Package
/// Management (<c>Directory.Packages.props</c>).
/// </summary>
public static class CentralPackageManagementRunHelper
{
    /// <summary>
    /// The props handling for a repository that uses Central Package Management. Missing props are
    /// created without versions, and the versions go into the existing
    /// <c>Directory.Packages.props</c>, which is also where distribution-owned packages are bumped.
    /// </summary>
    internal static void HandleDirectoryBuildPropsFiles(
        ILogger logger,
        DirectoryInfo projectPath,
        OptionsFile options,
        RunSummary summary,
        string rawCodingRulesDistributionProjectTargetBaseUrl,
        CentralPackageManagementContext centralPackageManagement)
    {
        logger.LogInformation($"{AppEmojisConstants.AreaDirectoryBuildProps} Central Package Management detected - package versions are maintained in {CentralPackageManagementHelper.PackagesPropsFileName}");
        summary.CentralPackageManagement = true;

        var requiredEntries = new List<CpmRequiredEntry>();
        var ownedPackageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var areas = new List<(string Area, DirectoryInfo Path, string UrlPart)> { ("root", projectPath, string.Empty) };
        areas.AddRange(ProjectHelper.EnumerateMappedPaths(options).Select(x => (x.Area, x.Path, x.Area)));

        foreach (var (area, path, urlPart) in areas)
        {
            var contentGit = DirectoryBuildPropsHelper.GetDistributionContent(
                logger,
                rawCodingRulesDistributionProjectTargetBaseUrl,
                urlPart,
                options.UseLatestMinorNugetVersion,
                options.ForceNugetRefresh);

            if (contentGit is null)
            {
                logger.LogWarning($"{Emoji.Known.Warning}   {area}: {DirectoryBuildPropsHelper.FileName} skipped — upstream content is empty");
                ProjectHelper.Record(summary, area, path, DirectoryBuildPropsResult.From(FileUpdateOutcome.Skipped));
                continue;
            }

            foreach (var item in PackageItemParser.Parse(contentGit) ?? [])
            {
                ownedPackageIds.Add(item.Id);
            }

            ProjectHelper.Record(
                summary,
                area,
                path,
                DirectoryBuildPropsHelper.HandleFileWithCentralPackageManagement(
                    logger,
                    area,
                    contentGit,
                    path,
                    centralPackageManagement,
                    requiredEntries,
                    options.DryRun));
        }

        HandleDirectoryPackagesPropsFile(logger, projectPath, options, summary, centralPackageManagement, requiredEntries, ownedPackageIds);
    }

    /// <summary>
    /// Bumps the distribution-owned entries of <c>Directory.Packages.props</c> and appends the
    /// entries that props files created in this run need.
    /// </summary>
    private static void HandleDirectoryPackagesPropsFile(
        ILogger logger,
        DirectoryInfo projectPath,
        OptionsFile options,
        RunSummary summary,
        CentralPackageManagementContext centralPackageManagement,
        IReadOnlyList<CpmRequiredEntry> requiredEntries,
        IReadOnlyCollection<string> ownedPackageIds)
    {
        Func<string, Version?>? resolveLatestVersion = null;
        if (options.UseLatestMinorNugetVersion)
        {
            AtcApiNugetClientHelper.Prefetch(logger, ownedPackageIds, options.ForceNugetRefresh, CancellationToken.None);
            resolveLatestVersion = packageId => AtcApiNugetClientHelper.GetLatestVersionForPackageId(
                logger,
                packageId,
                options.ForceNugetRefresh,
                CancellationToken.None);
        }

        var packagesPropsResult = DirectoryPackagesPropsHelper.HandleFile(
            logger,
            centralPackageManagement,
            requiredEntries,
            ownedPackageIds,
            resolveLatestVersion,
            options.DryRun);

        summary.Files.Add(new RunFileResult(
            "root",
            CentralPackageManagementHelper.PackagesPropsFileName,
            Path.Combine(projectPath.FullName, CentralPackageManagementHelper.PackagesPropsFileName),
            packagesPropsResult.Outcome));

        foreach (var bump in packagesPropsResult.PackageBumps)
        {
            summary.PackageBumps.Add(new RunPackageBump(
                bump.PackageId,
                bump.Version.ToString(),
                bump.NewestVersion.ToString()));
        }
    }
}