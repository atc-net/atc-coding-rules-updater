namespace Atc.CodingRules.Updater.CentralPackageManagement;

/// <summary>
/// Maintains the distribution-owned entries of an existing <c>Directory.Packages.props</c>.
/// </summary>
/// <remarks>
/// The file belongs to the repository, so the updater only ever makes two kinds of change:
/// it raises the version of an entry whose id the distribution owns, and it appends entries that
/// props files it created in the same run need in order to restore. Nothing is removed,
/// reordered or reformatted, and entries the distribution does not own are never touched.
/// </remarks>
public static class DirectoryPackagesPropsHelper
{
    /// <summary>
    /// Bumps owned entries and appends missing required ones.
    /// </summary>
    /// <param name="content">The current <c>Directory.Packages.props</c> content.</param>
    /// <param name="requiredEntries">Entries that must exist; ones already present (by id, any item type, any group) are left alone.</param>
    /// <param name="ownedPackageIds">Package ids the distribution owns. Only these are bumped.</param>
    /// <param name="resolveLatestVersion">Returns the newest version of a package, or <c>null</c> to skip bumping.</param>
    public static DirectoryPackagesPropsUpdate Apply(
        string content,
        IReadOnlyList<CpmRequiredEntry> requiredEntries,
        IReadOnlyCollection<string> ownedPackageIds,
        Func<string, Version?>? resolveLatestVersion)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(requiredEntries);
        ArgumentNullException.ThrowIfNull(ownedPackageIds);

        var (bumpedContent, bumps) = resolveLatestVersion is null
            ? (content, [])
            : BumpOwnedEntries(content, ownedPackageIds, resolveLatestVersion);

        var (appendedContent, appended) = AppendMissingEntries(bumpedContent, requiredEntries);

        return new DirectoryPackagesPropsUpdate(appendedContent, bumps, appended);
    }

    public static DirectoryBuildPropsResult HandleFile(
        ILogger logger,
        CentralPackageManagementContext context,
        IReadOnlyList<CpmRequiredEntry> requiredEntries,
        IReadOnlyCollection<string> ownedPackageIds,
        Func<string, Version?>? resolveLatestVersion,
        bool dryRun = false)
    {
        ArgumentNullException.ThrowIfNull(context);

        const string descriptionPart = $"[yellow]root: [/]{CentralPackageManagementHelper.PackagesPropsFileName}";

        var file = context.PackagesPropsFile;
        if (file is null || !file.Exists)
        {
            logger.LogInformation($"{EmojisConstants.FileNotUpdated}   {descriptionPart} skipped — the file does not exist");
            return DirectoryBuildPropsResult.From(FileUpdateOutcome.Skipped);
        }

        var content = FileHelper.ReadAllText(file);
        var update = Apply(content, requiredEntries, ownedPackageIds, resolveLatestVersion);

        foreach (var bump in update.Bumps)
        {
            logger.LogDebug($"{AppEmojisConstants.PackageReference}   {bump.PackageId} @ {bump.Version} => {bump.NewestVersion}");
        }

        foreach (var entry in update.Appended)
        {
            logger.LogDebug($"{AppEmojisConstants.PackageReference}   {entry.ItemType} {Markup.Escape(entry.Id)} @ {Markup.Escape(entry.Version)} added");
        }

        if (update.Content.Equals(content, StringComparison.Ordinal))
        {
            logger.LogInformation($"{EmojisConstants.FileNotUpdated}   {descriptionPart} nothing to update");
            return DirectoryBuildPropsResult.From(FileUpdateOutcome.Unchanged);
        }

        if (dryRun)
        {
            logger.LogInformation($"{EmojisConstants.FileUpdated}   [dim](dry-run)[/] would update {descriptionPart}");
        }
        else
        {
            FileHelper.WriteAllTextPreservingByteOrderMark(file.FullName, update.Content);
            logger.LogInformation($"{EmojisConstants.FileUpdated}   {descriptionPart} updated");
        }

        return new DirectoryBuildPropsResult(FileUpdateOutcome.Updated, update.Bumps, DirectoryBuildPropsDrift.Empty);
    }

    private static (string Content, List<DotnetNugetPackage> Bumps) BumpOwnedEntries(
        string content,
        IReadOnlyCollection<string> ownedPackageIds,
        Func<string, Version?> resolveLatestVersion)
    {
        var owned = new HashSet<string>(ownedPackageIds, StringComparer.OrdinalIgnoreCase);
        var bumps = new List<DotnetNugetPackage>();

        var candidates = (PackageItemParser.Parse(content) ?? [])
            .Where(x => x.ItemType is PackageItemType.PackageVersion or PackageItemType.GlobalPackageReference &&
                        owned.Contains(x.Id) &&
                        !x.IsConditional &&
                        !x.IsUpdate)
            .ToList();

        foreach (var candidate in candidates)
        {
            // Prerelease, floating, range and $(Property) versions cannot be compared.
            if (!Version.TryParse(candidate.Version, out var current))
            {
                continue;
            }

            var latest = resolveLatestVersion(candidate.Id);
            if (latest is null ||
                !latest.IsNewerThan(current, withinMinorReleaseOnly: true))
            {
                continue;
            }

            // Versions are replaced in place, so line numbers stay valid; re-read the item to be
            // safe against several items sharing a line.
            var item = PackageItemParser.Parse(content)!
                .First(x => x.ItemType == candidate.ItemType &&
                            x.Id.Equals(candidate.Id, StringComparison.Ordinal) &&
                            x.LineNumber == candidate.LineNumber);

            content = MsBuildFileTextEditor.SetVersion(content, item, latest.ToString());
            bumps.Add(new DotnetNugetPackage(candidate.Id, current, latest));
        }

        return (content, bumps);
    }

    private static (string Content, List<CpmRequiredEntry> Appended) AppendMissingEntries(
        string content,
        IReadOnlyList<CpmRequiredEntry> requiredEntries)
    {
        var existingIds = (PackageItemParser.Parse(content) ?? [])
            .Where(x => x.ItemType is PackageItemType.PackageVersion or PackageItemType.GlobalPackageReference)
            .Select(x => x.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missing = requiredEntries
            .Where(x => !existingIds.Contains(x.Id))
            .DistinctBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var group in missing.GroupBy(x => x.GroupLabel, StringComparer.Ordinal))
        {
            content = MsBuildFileTextEditor.AppendItems(
                content,
                group.Key,
                group.Select(x => $"<{x.ItemType} Include=\"{x.Id}\" Version=\"{x.Version}\" />").ToList());
        }

        return (content, missing);
    }
}