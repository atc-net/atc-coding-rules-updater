namespace Atc.CodingRules.Updater.CentralPackageManagement.Migration;

/// <summary>
/// Plans the conversion of a repository to Central Package Management (CPM).
/// </summary>
/// <remarks>
/// <para>
/// The planner only reads. It returns the new content of every file it would change, so the
/// same plan serves <c>--dry-run</c> and the real migration.
/// </para>
/// <para>
/// The plan preserves what every project restores: when projects use different versions of a
/// package, the highest becomes the central version and the others keep theirs through
/// <c>VersionOverride</c>, unless unifying is asked for. No versions are bumped, and transitive
/// pinning is not switched on, because both would change what restores.
/// </para>
/// </remarks>
public static class CpmMigrationPlanner
{
    /// <summary>The label of the group holding everything that is not a code analyzer.</summary>
    public const string PackagesGroupLabel = "Packages";

    private static readonly string[] AnalyzersMovedComment =
    [
        "Shared code analyzers are declared as GlobalPackageReference items in",
        "Directory.Packages.props (Central Package Management).",
    ];

    /// <summary>
    /// Plans the migration of the repository at <paramref name="projectPath"/>.
    /// </summary>
    /// <param name="projectPath">The repository root.</param>
    /// <param name="unifyVersions">When <c>true</c>, every project gets the highest version and no <c>VersionOverride</c> is written. This changes what restores.</param>
    public static CpmMigrationPlan Plan(
        DirectoryInfo projectPath,
        bool unifyVersions = false)
    {
        ArgumentNullException.ThrowIfNull(projectPath);

        var context = CentralPackageManagementHelper.Detect(projectPath);
        if (context.IsEnabled)
        {
            return new CpmMigrationPlan { AlreadyMigrated = true };
        }

        var blockers = new List<CpmMigrationBlocker>();
        if (context.HasPackagesPropsWithoutFlag)
        {
            blockers.Add(new CpmMigrationBlocker(
                "PackagesPropsWithoutFlag",
                $"{CentralPackageManagementHelper.PackagesPropsFileName} already exists without ManagePackageVersionsCentrally; decide what it is for before migrating",
                Path.Combine(projectPath.FullName, CentralPackageManagementHelper.PackagesPropsFileName)));
        }

        CollectRepositoryBlockers(projectPath, blockers);

        var references = ReadReferences(projectPath);
        var migratable = ClassifyReferences(references, blockers);

        var centralVersions = ResolveCentralVersions(migratable, blockers);
        if (blockers.Count > 0)
        {
            return new CpmMigrationPlan { Blockers = blockers };
        }

        var globalIds = ResolveGlobalAnalyzerIds(projectPath, migratable);
        return BuildPlan(projectPath, references, centralVersions, globalIds, unifyVersions);
    }

    /// <summary>
    /// Compares two NuGet version strings. Returns <c>null</c> when either is not a plain
    /// version (a property, a range, …).
    /// </summary>
    internal static int? CompareVersions(
        string a,
        string b)
    {
        var parsedA = ParseVersion(a);
        var parsedB = ParseVersion(b);
        if (parsedA is null || parsedB is null)
        {
            return null;
        }

        var numeric = parsedA.Value.Numeric.CompareTo(parsedB.Value.Numeric);
        if (numeric != 0)
        {
            return numeric;
        }

        // A release sorts above any prerelease of the same version.
        return (parsedA.Value.Prerelease, parsedB.Value.Prerelease) switch
        {
            (null, null) => 0,
            (null, _) => 1,
            (_, null) => -1,
            var (x, y) => string.CompareOrdinal(x, y),
        };
    }

    private static (Version Numeric, string? Prerelease)? ParseVersion(
        string value)
    {
        var dashIndex = value.IndexOf('-', StringComparison.Ordinal);
        var numericPart = dashIndex < 0 ? value : value[..dashIndex];
        var prerelease = dashIndex < 0 ? null : value[(dashIndex + 1)..];

        if (!numericPart.Contains('.', StringComparison.Ordinal))
        {
            numericPart += ".0";
        }

        return Version.TryParse(numericPart, out var numeric)
            ? (numeric, prerelease)
            : null;
    }

    private static void CollectRepositoryBlockers(
        DirectoryInfo projectPath,
        List<CpmMigrationBlocker> blockers)
    {
        foreach (var file in FileHelper.EnumerateSourceFiles(projectPath, "packages.config"))
        {
            blockers.Add(new CpmMigrationBlocker(
                "PackagesConfig",
                "A packages.config project cannot use Central Package Management; migrate it to PackageReference first",
                file.FullName));
        }

        var handledFileNames = new[]
        {
            DirectoryBuildPropsHelper.FileName,
            "Directory.Build.targets",
            CentralPackageManagementHelper.PackagesPropsFileName,
        };

        var otherImportedFiles = FileHelper.EnumerateSourceFiles(projectPath, "*.props")
            .Concat(FileHelper.EnumerateSourceFiles(projectPath, "*.targets"))
            .Where(x => !handledFileNames.Contains(x.Name, StringComparer.OrdinalIgnoreCase));

        foreach (var file in otherImportedFiles)
        {
            var items = PackageItemParser.Parse(FileHelper.ReadAllText(file)) ?? [];
            if (items.Any(x => x.ItemType == PackageItemType.PackageReference && x.Version is not null))
            {
                blockers.Add(new CpmMigrationBlocker(
                    "PackageReferenceInOtherImportedFile",
                    "Versioned PackageReference items in an imported file are not migrated automatically; move them to a project or Directory.Build.props first",
                    file.FullName));
            }
        }
    }

    private static List<(FileInfo File, PackageItem Item)> ReadReferences(
        DirectoryInfo projectPath)
        => FileHelper.EnumerateSourceFiles(projectPath, "*.csproj")
            .Concat(FileHelper.EnumerateSourceFiles(projectPath, DirectoryBuildPropsHelper.FileName))
            .Concat(FileHelper.EnumerateSourceFiles(projectPath, "Directory.Build.targets"))
            .SelectMany(file => (PackageItemParser.Parse(FileHelper.ReadAllText(file)) ?? [])
                .Where(x => x.ItemType == PackageItemType.PackageReference)
                .Select(item => (file, item)))
            .ToList();

    private static List<(FileInfo File, PackageItem Item)> ClassifyReferences(
        List<(FileInfo File, PackageItem Item)> references,
        List<CpmMigrationBlocker> blockers)
    {
        var migratable = new List<(FileInfo File, PackageItem Item)>();
        foreach (var (file, item) in references)
        {
            var blocker = item switch
            {
                { IsImplicitlyDefined: true } => null,
                { IsUpdate: true } => new CpmMigrationBlocker("PackageReferenceUpdate", $"PackageReference Update=\"{item.Id}\" is not migrated automatically", file.FullName),
                { VersionOverride: not null } => new CpmMigrationBlocker("VersionOverrideExists", $"{item.Id} already uses VersionOverride, which suggests a half-finished migration", file.FullName),
                { Version: null } => new CpmMigrationBlocker("MissingVersion", $"{item.Id} has no version to centralise", file.FullName),
                _ when item.Version!.Contains('*', StringComparison.Ordinal) => new CpmMigrationBlocker("FloatingVersion", $"{item.Id} uses the floating version {item.Version}, which Central Package Management does not allow by default", file.FullName),
                _ => null,
            };

            if (blocker is not null)
            {
                blockers.Add(blocker);
            }
            else if (!item.IsImplicitlyDefined)
            {
                migratable.Add((file, item));
            }
        }

        foreach (var group in migratable.GroupBy(x => x.Item.Id, StringComparer.OrdinalIgnoreCase))
        {
            if (group.Any(x => x.Item.IsConditional) &&
                group.Select(x => x.Item.Version).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
            {
                blockers.Add(new CpmMigrationBlocker(
                    "ConditionalVersionsDiffer",
                    $"{group.Key} has conditional references with different versions; conditional PackageVersion entries are not generated",
                    group.First().File.FullName));
            }
        }

        return migratable;
    }

    private static Dictionary<string, string> ResolveCentralVersions(
        List<(FileInfo File, PackageItem Item)> migratable,
        List<CpmMigrationBlocker> blockers)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in migratable.GroupBy(x => x.Item.Id, StringComparer.OrdinalIgnoreCase))
        {
            var versions = group.Select(x => x.Item.Version!).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var highest = versions[0];
            foreach (var version in versions.Skip(1))
            {
                var comparison = CompareVersions(version, highest);
                if (comparison is null)
                {
                    blockers.Add(new CpmMigrationBlocker(
                        "DivergentNonComparableVersions",
                        $"{group.Key} is referenced with different versions that cannot be compared ({string.Join(", ", versions)})",
                        group.First().File.FullName));
                    break;
                }

                if (comparison > 0)
                {
                    highest = version;
                }
            }

            result[group.Key] = highest;
        }

        return result;
    }

    /// <summary>
    /// The root analyzers that become <c>GlobalPackageReference</c> items: those in the root
    /// <c>Code Analyzers</c> group that nothing else references, when the SDK supports it.
    /// </summary>
    private static HashSet<string> ResolveGlobalAnalyzerIds(
        DirectoryInfo projectPath,
        List<(FileInfo File, PackageItem Item)> migratable)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!CentralPackageManagementHelper.SupportsGlobalPackageReference(projectPath))
        {
            return result;
        }

        var rootProps = Path.Combine(projectPath.FullName, DirectoryBuildPropsHelper.FileName);
        var analyzers = migratable
            .Where(x => x.File.FullName.Equals(rootProps, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(x.Item.ItemGroupLabel, CpmContentTransformer.CodeAnalyzersLabel, StringComparison.Ordinal))
            .ToList();

        foreach (var analyzer in analyzers)
        {
            var referencedElsewhere = migratable.Any(x =>
                x.Item.Id.Equals(analyzer.Item.Id, StringComparison.OrdinalIgnoreCase) &&
                !x.File.FullName.Equals(rootProps, StringComparison.OrdinalIgnoreCase));

            if (!referencedElsewhere && !analyzer.Item.IsConditional)
            {
                result.Add(analyzer.Item.Id);
            }
        }

        // The group is replaced as a whole, so only go global when every analyzer in it can.
        return analyzers.All(x => result.Contains(x.Item.Id))
            ? result
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    private static CpmMigrationPlan BuildPlan(
        DirectoryInfo projectPath,
        List<(FileInfo File, PackageItem Item)> references,
        Dictionary<string, string> centralVersions,
        HashSet<string> globalIds,
        bool unifyVersions)
    {
        var fileChanges = new Dictionary<string, string>(StringComparer.Ordinal);
        var overrides = new List<CpmMigrationOverride>();
        var rootProps = Path.Combine(projectPath.FullName, DirectoryBuildPropsHelper.FileName);

        foreach (var file in references.Select(x => x.File.FullName).Distinct(StringComparer.Ordinal))
        {
            var content = FileHelper.ReadAllText(new FileInfo(file));
            var original = content;

            if (globalIds.Count > 0 && file.Equals(rootProps, StringComparison.OrdinalIgnoreCase))
            {
                content = MsBuildFileTextEditor.ReplaceItemGroup(content, CpmContentTransformer.CodeAnalyzersLabel, AnalyzersMovedComment);
            }

            // Each edit shifts later elements, so re-parse and handle one reference at a time.
            while (NextVersionedReference(content) is { } item)
            {
                if (unifyVersions ||
                    item.Version!.Equals(centralVersions[item.Id], StringComparison.OrdinalIgnoreCase))
                {
                    content = MsBuildFileTextEditor.RemoveVersion(content, item);
                }
                else
                {
                    overrides.Add(new CpmMigrationOverride(file, item.Id, item.Version));
                    content = MsBuildFileTextEditor.ReplaceVersionWithVersionOverride(content, item);
                }
            }

            if (!content.Equals(original, StringComparison.Ordinal))
            {
                fileChanges[file] = content;
            }
        }

        var packages = centralVersions
            .Select(x => new CpmMigrationPackage(
                x.Key,
                x.Value,
                globalIds.Contains(x.Key) ? PackageItemType.GlobalPackageReference : PackageItemType.PackageVersion))
            .ToList();

        if (fileChanges.Count > 0)
        {
            var newLine = fileChanges.Values.First().Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
            fileChanges[Path.Combine(projectPath.FullName, CentralPackageManagementHelper.PackagesPropsFileName)] =
                RenderPackagesProps(packages, newLine);
        }

        return new CpmMigrationPlan
        {
            Packages = packages,
            Overrides = overrides,
            FileChanges = fileChanges,
        };
    }

    private static PackageItem? NextVersionedReference(string content)
        => PackageItemParser.Parse(content)?.FirstOrDefault(x =>
            x.ItemType == PackageItemType.PackageReference &&
            x.Version is not null &&
            !x.IsImplicitlyDefined);

    private static string RenderPackagesProps(
        List<CpmMigrationPackage> packages,
        string newLine)
    {
        var sb = new StringBuilder();
        sb.Append("<Project>").Append(newLine).Append(newLine);
        sb.Append("  <PropertyGroup>").Append(newLine);
        sb.Append("    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>").Append(newLine);
        sb.Append("  </PropertyGroup>").Append(newLine);

        AppendGroup(sb, CpmContentTransformer.GroupLabelPrefix + CpmContentTransformer.CodeAnalyzersLabel, packages.Where(x => x.ItemType == PackageItemType.GlobalPackageReference), newLine);
        AppendGroup(sb, PackagesGroupLabel, packages.Where(x => x.ItemType == PackageItemType.PackageVersion).OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase), newLine);

        sb.Append(newLine).Append("</Project>").Append(newLine);
        return sb.ToString();
    }

    private static void AppendGroup(
        StringBuilder sb,
        string label,
        IEnumerable<CpmMigrationPackage> packages,
        string newLine)
    {
        var items = packages.ToList();
        if (items.Count == 0)
        {
            return;
        }

        sb.Append(newLine).Append("  <ItemGroup Label=\"").Append(label).Append("\">").Append(newLine);
        foreach (var package in items)
        {
            sb.Append("    <")
                .Append(package.ItemType.ToString())
                .Append(" Include=\"").Append(EscapeAttribute(package.Id))
                .Append("\" Version=\"").Append(EscapeAttribute(package.Version))
                .Append("\" />").Append(newLine);
        }

        sb.Append("  </ItemGroup>").Append(newLine);
    }

    private static string EscapeAttribute(string value)
        => value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal);
}