// ReSharper disable InvertIf
// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
namespace Atc.CodingRules.Updater;

/// <summary>
/// Downloads <c>Directory.Build.props</c> files from the atc-coding-rules distribution and
/// keeps the local copy in sync, optionally bumping NuGet package references to the latest minor
/// version available on the ATC nuget search service.
/// </summary>
public static class DirectoryBuildPropsHelper
{
    /// <summary>The standard MSBuild props file name picked up by .NET SDK projects.</summary>
    public const string FileName = "Directory.Build.props";

    /// <summary>
    /// Convenience wrapper around <see cref="FileHelper.SearchAllForElement(DirectoryInfo, string, string, string?, SearchOption, StringComparison)"/>
    /// scoped to <c>Directory.Build.props</c> files.
    /// </summary>
    public static Collection<FileInfo> SearchAllForElement(
        DirectoryInfo projectPath,
        string elementName,
        string? elementValue = null,
        SearchOption searchOption = SearchOption.AllDirectories,
        StringComparison stringComparison = StringComparison.Ordinal)
        => FileHelper.SearchAllForElement(
            projectPath,
            FileName,
            elementName,
            elementValue,
            searchOption,
            stringComparison);

    /// <summary>
    /// Downloads the upstream <c>Directory.Build.props</c> for <paramref name="urlPart"/> and
    /// either creates the local file or replaces it (after applying optional package-version bumps).
    /// </summary>
    /// <param name="logger">Where progress is reported.</param>
    /// <param name="area">Short label used in error logs (<c>"root"</c>, <c>"src"</c>, …).</param>
    /// <param name="rawCodingRulesDistribution">Base raw URL for the chosen project target's distribution folder.</param>
    /// <param name="useLatestMinorNugetVersion">When <c>true</c>, every <c>&lt;PackageReference&gt;</c> in the upstream content is bumped to the latest minor version reported by the ATC nuget search service.</param>
    /// <param name="path">Local directory that should end up containing the props file.</param>
    /// <param name="urlPart">Sub-path appended to <paramref name="rawCodingRulesDistribution"/> (empty for root).</param>
    /// <param name="dryRun">When <c>true</c>, log what would change without writing any files.</param>
    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "Covers the create / update / dry-run paths with shared setup; splitting hurts readability.")]
    public static void HandleFile(
        ILogger logger,
        string area,
        string rawCodingRulesDistribution,
        bool useLatestMinorNugetVersion,
        DirectoryInfo path,
        string urlPart,
        bool dryRun = false)
    {
        ArgumentNullException.ThrowIfNull(path);

        var descriptionPart = string.IsNullOrEmpty(urlPart)
            ? $"[yellow]root: [/]{FileName}"
            : $"[yellow]{urlPart}: [/]{FileName}";

        var file = new FileInfo(Path.Combine(path.FullName, FileName));

        var rawGitUrl = string.IsNullOrEmpty(urlPart)
            ? $"{rawCodingRulesDistribution}/{FileName}"
            : $"{rawCodingRulesDistribution}/{urlPart}/{FileName}";

        var displayName = rawGitUrl.Replace(Constants.GitRawContentUrl, Constants.GitHubPrefix, StringComparison.Ordinal);

        try
        {
            if (!Directory.Exists(file.Directory!.FullName))
            {
                if (dryRun)
                {
                    logger.LogInformation($"{EmojisConstants.FileCreated}   [dim](dry-run)[/] would create directory {file.Directory!.FullName}");
                }
                else
                {
                    Directory.CreateDirectory(file.Directory.FullName);
                }
            }

            var contentGit = HttpClientHelper.GetAsString(logger, rawGitUrl, displayName);
            if (string.IsNullOrEmpty(contentGit))
            {
                logger.LogWarning($"{Emoji.Known.Warning}   {descriptionPart} skipped — upstream content is empty");
                return;
            }

            if (useLatestMinorNugetVersion)
            {
                contentGit = EnsureLatestPackageReferencesVersion(logger, contentGit, LogCategoryType.Trace);
            }

            if (!file.Exists)
            {
                if (dryRun)
                {
                    logger.LogInformation($"{EmojisConstants.FileCreated}   [dim](dry-run)[/] would create {descriptionPart}");
                    return;
                }

                FileHelper.CreateFile(logger, file, contentGit, descriptionPart);
                return;
            }

            var contentFile = FileHelper.ReadAllText(file);
            if (string.IsNullOrEmpty(contentFile))
            {
                if (dryRun)
                {
                    logger.LogInformation($"{EmojisConstants.FileCreated}   [dim](dry-run)[/] would create {descriptionPart}");
                    return;
                }

                FileHelper.CreateFile(logger, file, contentGit, descriptionPart);
                return;
            }

            if (contentGit.Equals(contentFile, StringComparison.Ordinal))
            {
                logger.LogInformation($"{EmojisConstants.FileNotUpdated}   {descriptionPart} nothing to update");
                return;
            }

            if (dryRun)
            {
                logger.LogInformation($"{EmojisConstants.FileUpdated}   [dim](dry-run)[/] would update {descriptionPart}");
                return;
            }

            UpdateFile(logger, file, contentFile, descriptionPart, useLatestMinorNugetVersion);
        }
        catch (Exception ex)
        {
            logger.LogError($"{EmojisConstants.Error} {Markup.Escape(area)} - {Markup.Escape(ex.Message)}");
            throw;
        }
    }

    /// <summary>
    /// Returns <c>true</c> when the props file in <paramref name="path"/> contains a placeholder
    /// element of the form <c>&lt;name&gt;&lt;!-- value --&gt;&lt;/name&gt;</c>.
    /// </summary>
    public static bool HasFileInsertPlaceholderElement(
        DirectoryInfo path,
        string elementName,
        string elementValue)
    {
        ArgumentNullException.ThrowIfNull(path);

        var file = new FileInfo(Path.Combine(path.FullName, FileName));
        if (!file.Exists)
        {
            return false;
        }

        var fileContent = FileHelper.ReadAllText(file);
        var searchText = $"<{elementName}><!-- {elementValue} --></{elementName}>";
        return fileContent.Contains(searchText, StringComparison.Ordinal);
    }

    /// <summary>
    /// Replaces a placeholder element <c>&lt;name&gt;&lt;!-- value --&gt;&lt;/name&gt;</c> in the props file
    /// at <paramref name="path"/> with <c>&lt;name&gt;newElementValue&lt;/name&gt;</c>. No-op if the file or
    /// the placeholder is missing.
    /// </summary>
    public static void UpdateFileInsertPlaceholderElement(
        ILogger logger,
        DirectoryInfo path,
        string elementName,
        string elementValue,
        string newElementValue)
    {
        ArgumentNullException.ThrowIfNull(path);

        var file = new FileInfo(Path.Combine(path.FullName, FileName));
        if (!file.Exists)
        {
            return;
        }

        var fileContent = FileHelper.ReadAllText(file);
        var searchText = $"<{elementName}><!-- {elementValue} --></{elementName}>";
        if (fileContent.Contains(searchText, StringComparison.Ordinal))
        {
            fileContent = fileContent.Replace(
                searchText,
                $"<{elementName}>{newElementValue}</{elementName}>",
                StringComparison.Ordinal);

            File.WriteAllText(file.FullName, fileContent);
            logger.LogDebug($"{EmojisConstants.FileUpdated}   {elementName} in file is updated to '{newElementValue}'");
        }
    }

    private static void UpdateFile(
        ILogger logger,
        FileInfo file,
        string fileContent,
        string descriptionPart,
        bool useLatestMinorNugetVersion)
    {
        if (useLatestMinorNugetVersion)
        {
            var newFileContent = EnsureLatestPackageReferencesVersion(logger, fileContent, LogCategoryType.Debug);
            if (!FileHelper.AreFilesEqual(fileContent, newFileContent) ||
                !fileContent.Equals(newFileContent, StringComparison.Ordinal))
            {
                fileContent = newFileContent;
            }
        }

        File.WriteAllText(file.FullName, fileContent);
        logger.LogInformation($"{EmojisConstants.FileUpdated}   {descriptionPart} updated");
    }

    private static string EnsureLatestPackageReferencesVersion(
        ILogger logger,
        string fileContent,
        LogCategoryType logCategoryType)
    {
        var packageReferencesThatNeedsToBeUpdated = GetPackageReferencesThatNeedsToBeUpdated(logger, fileContent);
        foreach (var item in packageReferencesThatNeedsToBeUpdated)
        {
            fileContent = fileContent.Replace(
                $"<PackageReference Include=\"{item.PackageId}\" Version=\"{item.Version}\"",
                $"<PackageReference Include=\"{item.PackageId}\" Version=\"{item.NewestVersion}\"",
                StringComparison.Ordinal);

            var logMessage = $"{AppEmojisConstants.PackageReference}   PackageReference {item.PackageId} @ {item.Version} => {item.NewestVersion}";
            switch (logCategoryType)
            {
                case LogCategoryType.Debug:
                    logger.LogDebug(logMessage);
                    break;
                case LogCategoryType.Trace:
                    logger.LogTrace(logMessage);
                    break;
                default:
                    throw new SwitchCaseDefaultException(logCategoryType);
            }
        }

        return fileContent;
    }

    private static List<DotnetNugetPackage> GetPackageReferencesThatNeedsToBeUpdated(
        ILogger logger,
        string fileContent)
    {
        var result = new List<DotnetNugetPackage>();

        var packageReferencesGit = DotnetNugetHelper.GetAllPackageReferences(fileContent);
        if (packageReferencesGit.Count > 0)
        {
            foreach (var item in packageReferencesGit)
            {
                if (Version.TryParse(item.Version, out var version))
                {
                    var latestVersion = AtcApiNugetClientHelper.GetLatestVersionForPackageId(logger, item.PackageId, CancellationToken.None);

                    if (latestVersion is not null &&
                        latestVersion.IsNewerThan(version, withinMinorReleaseOnly: true))
                    {
                        result.Add(
                            new DotnetNugetPackage(
                                item.PackageId,
                                version,
                                latestVersion));
                    }
                }
            }
        }

        return result;
    }
}