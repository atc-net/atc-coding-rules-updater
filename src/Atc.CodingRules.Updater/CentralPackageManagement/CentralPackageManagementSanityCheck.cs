namespace Atc.CodingRules.Updater.CentralPackageManagement;

/// <summary>
/// Pre-flight checks for repositories that use Central Package Management (CPM). Each error
/// matches a NuGet restore error, so a run stops before it writes into a repository that would
/// not restore anyway.
/// </summary>
internal static class CentralPackageManagementSanityCheck
{
    private const string VersionOnPackageReferenceMessage =
        "PackageReference items define a Version while Central Package Management is enabled (restore error NU1008). Move the version to Directory.Packages.props in the following files:";

    private const string MissingPackageVersionMessage =
        "PackageReference items have no matching PackageVersion in Directory.Packages.props (restore error NU1010). Add the missing PackageVersion entries for the following files:";

    public static void Collect(
        ICollection<SanityCheckDiagnostic> diagnostics,
        DirectoryInfo projectPath,
        CentralPackageManagementMode mode)
    {
        var context = CentralPackageManagementHelper.Detect(projectPath, mode);

        if (context.HasPackagesPropsWithoutFlag)
        {
            diagnostics.Add(new SanityCheckDiagnostic(
                SanityCheckSeverity.Warning,
                code: "CpmPackagesPropsWithoutFlag",
                message: $"/{CentralPackageManagementHelper.PackagesPropsFileName} exists but ManagePackageVersionsCentrally is not true, so the repository is treated as not using Central Package Management"));
        }

        if (!context.IsEnabled)
        {
            return;
        }

        var packagesPropsImportsOtherFiles = PackagesPropsImportsOtherFiles(context);
        var globalIds = context.Entries
            .Where(x => x.ItemType == PackageItemType.GlobalPackageReference)
            .Select(x => x.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var file in EnumerateReferencingFiles(projectPath))
        {
            CollectForFile(diagnostics, file, context, globalIds, packagesPropsImportsOtherFiles);
        }

        foreach (var duplicate in context.Entries
                     .Where(x => x.ItemType == PackageItemType.PackageVersion && globalIds.Contains(x.Id)))
        {
            diagnostics.Add(new SanityCheckDiagnostic(
                SanityCheckSeverity.Warning,
                code: "CpmDuplicateGlobalAndPackageReference",
                message: $"{duplicate.Id} is both a GlobalPackageReference and a PackageVersion in /{CentralPackageManagementHelper.PackagesPropsFileName}",
                filePath: context.PackagesPropsFile?.FullName));
        }
    }

    private static void CollectForFile(
        ICollection<SanityCheckDiagnostic> diagnostics,
        FileInfo file,
        CentralPackageManagementContext context,
        HashSet<string> globalIds,
        bool packagesPropsImportsOtherFiles)
    {
        var content = FileHelper.ReadAllText(file);

        // A project that opts out keeps versions on its references legitimately, so none of
        // the reference checks apply to it.
        if (file.Extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase) &&
            content.Contains("<ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>", StringComparison.OrdinalIgnoreCase))
        {
            diagnostics.Add(new SanityCheckDiagnostic(
                SanityCheckSeverity.Warning,
                code: "CpmProjectOptOut",
                message: "A project opts out of Central Package Management; the updater does not handle per-project opt-out",
                filePath: file.FullName));
            return;
        }

        var references = (PackageItemParser.Parse(content) ?? [])
            .Where(x => x.ItemType == PackageItemType.PackageReference && !x.IsImplicitlyDefined)
            .ToList();

        AddReferenceDiagnostic(
            diagnostics,
            file,
            references.Where(x => x.Version is not null).ToList(),
            "CpmVersionOnPackageReference",
            VersionOnPackageReferenceMessage);

        // A PackageVersion defined in a file that Directory.Packages.props imports cannot be
        // seen here, and reporting a false error would block the run.
        if (!packagesPropsImportsOtherFiles)
        {
            AddReferenceDiagnostic(
                diagnostics,
                file,
                references.Where(x => x.Version is null &&
                                      x.VersionOverride is null &&
                                      !x.IsUpdate &&
                                      !context.HasEntry(x.Id)).ToList(),
                "CpmMissingPackageVersion",
                MissingPackageVersionMessage);
        }

        foreach (var duplicate in references.Where(x => globalIds.Contains(x.Id)))
        {
            diagnostics.Add(new SanityCheckDiagnostic(
                SanityCheckSeverity.Warning,
                code: "CpmDuplicateGlobalAndPackageReference",
                message: $"{duplicate.Id} is a GlobalPackageReference and is also referenced by a PackageReference; the reference is redundant",
                filePath: file.FullName));
        }
    }

    /// <summary>
    /// Adds one diagnostic for <paramref name="offending"/> references in <paramref name="file"/>:
    /// an error when any of them is unconditional, otherwise a warning. A condition cannot be
    /// evaluated here (it may well exclude the item under CPM), and an error would block a
    /// repository that restores fine.
    /// </summary>
    private static void AddReferenceDiagnostic(
        ICollection<SanityCheckDiagnostic> diagnostics,
        FileInfo file,
        List<PackageItem> offending,
        string code,
        string message)
    {
        if (offending.Count == 0)
        {
            return;
        }

        var allConditional = offending.TrueForAll(x => x.IsConditional);
        diagnostics.Add(new SanityCheckDiagnostic(
            allConditional ? SanityCheckSeverity.Warning : SanityCheckSeverity.Error,
            code,
            allConditional ? $"{message.TrimEnd(':')} (conditional items, so this may be intended)" : message,
            filePath: file.FullName));
    }

    /// <summary>
    /// The files whose package references restore acts on: every project file and every
    /// <c>Directory.Build.props</c>, excluding build output.
    /// </summary>
    internal static IEnumerable<FileInfo> EnumerateReferencingFiles(
        DirectoryInfo projectPath)
        => FileHelper.EnumerateSourceFiles(projectPath, "*.csproj")
            .Concat(FileHelper.EnumerateSourceFiles(projectPath, DirectoryBuildPropsHelper.FileName));

    private static bool PackagesPropsImportsOtherFiles(
        CentralPackageManagementContext context)
    {
        if (context.PackagesPropsFile is null)
        {
            return false;
        }

        try
        {
            return XDocument
                .Parse(FileHelper.ReadAllText(context.PackagesPropsFile))
                .Descendants()
                .Any(x => x.Name.LocalName.Equals("Import", StringComparison.Ordinal));
        }
        catch (XmlException)
        {
            return false;
        }
    }
}