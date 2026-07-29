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
    /// <param name="forceNugetRefresh">When <c>true</c>, ask the ATC API to bypass its own 12-hour version cache.</param>
    /// <returns>What happened to the file, plus the package bumps and drift found, so callers can summarise the run.</returns>
    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "Covers the create / update / dry-run paths with shared setup; splitting hurts readability.")]
    public static DirectoryBuildPropsResult HandleFile(
        ILogger logger,
        string area,
        string rawCodingRulesDistribution,
        bool useLatestMinorNugetVersion,
        DirectoryInfo path,
        string urlPart,
        bool dryRun = false,
        bool forceNugetRefresh = false)
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
                    logger.LogInformation($"{EmojisConstants.FileCreated}   [dim](dry-run)[/] would create directory {file.Directory.FullName}");
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
                return DirectoryBuildPropsResult.From(FileUpdateOutcome.Skipped);
            }

            if (useLatestMinorNugetVersion)
            {
                contentGit = EnsureLatestPackageReferencesVersion(logger, contentGit, LogCategoryType.Trace, forceNugetRefresh);
            }

            if (!file.Exists)
            {
                if (dryRun)
                {
                    logger.LogInformation($"{EmojisConstants.FileCreated}   [dim](dry-run)[/] would create {descriptionPart}");
                    return DirectoryBuildPropsResult.From(FileUpdateOutcome.Created);
                }

                FileHelper.CreateFile(logger, file, contentGit, descriptionPart);
                return DirectoryBuildPropsResult.From(FileUpdateOutcome.Created);
            }

            var contentFile = FileHelper.ReadAllText(file);
            if (string.IsNullOrEmpty(contentFile))
            {
                if (dryRun)
                {
                    logger.LogInformation($"{EmojisConstants.FileCreated}   [dim](dry-run)[/] would create {descriptionPart}");
                    return DirectoryBuildPropsResult.From(FileUpdateOutcome.Created);
                }

                FileHelper.CreateFile(logger, file, contentGit, descriptionPart);
                return DirectoryBuildPropsResult.From(FileUpdateOutcome.Created);
            }

            if (contentGit.Equals(contentFile, StringComparison.Ordinal))
            {
                logger.LogInformation($"{EmojisConstants.FileNotUpdated}   {descriptionPart} nothing to update");
                return DirectoryBuildPropsResult.From(FileUpdateOutcome.Unchanged);
            }

            // An existing props file is never overwritten from the distribution, so upstream
            // additions would otherwise be adopted by nobody and reported to no one. Report them
            // before the dry-run short-circuit so --dry-run surfaces them too.
            var drift = GetDrift(contentGit, contentFile);
            LogDrift(logger, drift, descriptionPart);

            var bumps = useLatestMinorNugetVersion
                ? GetPackageReferencesThatNeedsToBeUpdated(logger, contentFile, forceNugetRefresh)
                : [];

            if (dryRun)
            {
                // Ask the same question UpdateFile would, so --dry-run cannot promise an update
                // that the real run would then report as "nothing to update".
                logger.LogInformation(bumps.Count > 0
                    ? $"{EmojisConstants.FileUpdated}   [dim](dry-run)[/] would update {descriptionPart}"
                    : $"{EmojisConstants.FileNotUpdated}   {descriptionPart} nothing to update");

                return new DirectoryBuildPropsResult(
                    bumps.Count > 0 ? FileUpdateOutcome.Updated : FileUpdateOutcome.Unchanged,
                    bumps,
                    drift);
            }

            var outcome = UpdateFile(logger, file, contentFile, descriptionPart, useLatestMinorNugetVersion, forceNugetRefresh);
            return new DirectoryBuildPropsResult(outcome, bumps, drift);
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

    /// <summary>
    /// Compares which package references and MSBuild properties exist in the upstream and local
    /// <c>Directory.Build.props</c>. Values are deliberately not compared — see
    /// <see cref="DirectoryBuildPropsDrift"/>.
    /// </summary>
    /// <returns><see cref="DirectoryBuildPropsDrift.Empty"/> when either side is not parseable XML.</returns>
    internal static DirectoryBuildPropsDrift GetDrift(
        string contentGit,
        string contentFile)
    {
        var upstream = ParseProjectElements(contentGit);
        var local = ParseProjectElements(contentFile);

        if (upstream is null || local is null)
        {
            return DirectoryBuildPropsDrift.Empty;
        }

        return new DirectoryBuildPropsDrift(
            OnlyIn(upstream.Value.PackageIds, local.Value.PackageIds),
            OnlyIn(local.Value.PackageIds, upstream.Value.PackageIds),
            OnlyIn(upstream.Value.PropertyNames, local.Value.PropertyNames));
    }

    /// <summary>
    /// Reports <paramref name="drift"/>: a one-line summary at information level, then one line
    /// per difference at debug level. No-op when there is nothing to report.
    /// </summary>
    internal static void LogDrift(
        ILogger logger,
        DirectoryBuildPropsDrift drift,
        string descriptionPart)
    {
        ArgumentNullException.ThrowIfNull(drift);

        if (!drift.HasDrift)
        {
            return;
        }

        logger.LogInformation(
            $"{AppEmojisConstants.Drift}   {descriptionPart} differs from the distribution in {drift.Count} place(s) - not applied, since the local file is never overwritten");

        foreach (var packageId in drift.PackageReferencesOnlyUpstream)
        {
            logger.LogDebug($"     - PackageReference only in distribution: {packageId}");
        }

        foreach (var packageId in drift.PackageReferencesOnlyLocal)
        {
            logger.LogDebug($"     - PackageReference only in local file: {packageId}");
        }

        foreach (var propertyName in drift.PropertiesOnlyUpstream)
        {
            logger.LogDebug($"     - Property only in distribution: {propertyName}");
        }
    }

    private static List<string> OnlyIn(
        IEnumerable<string> source,
        IEnumerable<string> other)
        => source
            .Except(other, StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>
    /// Extracts package ids and MSBuild property names from props content, or <c>null</c> when the
    /// content is not parseable XML. Element names are matched on their local name so a props file
    /// carrying the legacy MSBuild namespace is handled too.
    /// </summary>
    private static (List<string> PackageIds, List<string> PropertyNames)? ParseProjectElements(
        string content)
    {
        XDocument document;
        try
        {
            document = XDocument.Parse(content);
        }
        catch (XmlException)
        {
            return null;
        }

        var packageIds = document
            .Descendants()
            .Where(x => x.Name.LocalName.Equals("PackageReference", StringComparison.Ordinal))
            .Select(x => x.Attribute("Include")?.Value)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var propertyNames = document
            .Descendants()
            .Where(x => x.Parent is not null &&
                        x.Parent.Name.LocalName.Equals("PropertyGroup", StringComparison.Ordinal))
            .Select(x => x.Name.LocalName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return (packageIds, propertyNames);
    }

    internal static FileUpdateOutcome UpdateFile(
        ILogger logger,
        FileInfo file,
        string fileContent,
        string descriptionPart,
        bool useLatestMinorNugetVersion,
        bool forceNugetRefresh = false)
    {
        var newFileContent = useLatestMinorNugetVersion
            ? EnsureLatestPackageReferencesVersion(logger, fileContent, LogCategoryType.Debug, forceNugetRefresh)
            : fileContent;

        // The upstream props content is deliberately not applied to an existing local file (it
        // carries user-specific values), so the only change this method can make is a package
        // bump. Without one there is nothing to write, and claiming "updated" would be a lie.
        if (newFileContent.Equals(fileContent, StringComparison.Ordinal))
        {
            logger.LogInformation($"{EmojisConstants.FileNotUpdated}   {descriptionPart} nothing to update");
            return FileUpdateOutcome.Unchanged;
        }

        File.WriteAllText(file.FullName, newFileContent);
        logger.LogInformation($"{EmojisConstants.FileUpdated}   {descriptionPart} updated");
        return FileUpdateOutcome.Updated;
    }

    private static string EnsureLatestPackageReferencesVersion(
        ILogger logger,
        string fileContent,
        LogCategoryType logCategoryType,
        bool forceNugetRefresh)
    {
        var packageReferencesThatNeedsToBeUpdated = GetPackageReferencesThatNeedsToBeUpdated(logger, fileContent, forceNugetRefresh);
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

    internal static List<DotnetNugetPackage> GetPackageReferencesThatNeedsToBeUpdated(
        ILogger logger,
        string fileContent,
        bool forceNugetRefresh = false)
    {
        var result = new List<DotnetNugetPackage>();

        var packageReferencesGit = DotnetNugetHelper.GetAllPackageReferences(fileContent);
        if (packageReferencesGit.Count > 0)
        {
            // Resolve every comparable package in one concurrent batch; the loop below then hits
            // the process cache instead of paying a serial round-trip per package.
            AtcApiNugetClientHelper.Prefetch(
                logger,
                packageReferencesGit
                    .Where(x => Version.TryParse(x.Version, out _))
                    .Select(x => x.PackageId),
                forceNugetRefresh,
                CancellationToken.None);

            foreach (var item in packageReferencesGit)
            {
                if (!Version.TryParse(item.Version, out var version))
                {
                    // Prerelease ("1.2.3-beta"), floating ("3.0.*") and MSBuild-property
                    // ("$(SomeVersion)") references cannot be compared as a System.Version.
                    // Skipping them is correct, but doing it silently left users with no way to
                    // tell the package had never been considered.
                    logger.LogTrace($"     Skipping {item.PackageId} @ {item.Version} - version is not comparable");
                    continue;
                }

                var latestVersion = AtcApiNugetClientHelper.GetLatestVersionForPackageId(logger, item.PackageId, forceNugetRefresh, CancellationToken.None);

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

        return result;
    }
}