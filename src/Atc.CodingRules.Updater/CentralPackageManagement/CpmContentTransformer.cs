namespace Atc.CodingRules.Updater.CentralPackageManagement;

/// <summary>
/// Translates a distributed <c>Directory.Build.props</c> into the Central Package Management
/// (CPM) layout.
/// </summary>
/// <remarks>
/// The mapping is decided by the <c>ItemGroup</c> label, never by the <c>PrivateAssets</c>
/// attribute. <c>xunit.runner.visualstudio</c> in the test props carries <c>PrivateAssets=all</c>,
/// and an attribute-based rule would promote it to a <c>GlobalPackageReference</c>, which would
/// push a test runner into every project.
/// </remarks>
public static class CpmContentTransformer
{
    /// <summary>
    /// The label of the distribution's analyzer group. The atc-coding-rules repository keeps this
    /// label stable because this mapping depends on it.
    /// </summary>
    public const string CodeAnalyzersLabel = "Code Analyzers";

    /// <summary>Prefix of the labels the updater gives the groups it appends to <c>Directory.Packages.props</c>.</summary>
    public const string GroupLabelPrefix = "atc-coding-rules: ";

    private static readonly string[] AnalyzersMovedComment =
    [
        "Shared code analyzers are declared as GlobalPackageReference items in",
        "Directory.Packages.props (Central Package Management).",
    ];

    private static readonly string[] AnalyzersSkippedComment =
    [
        "Shared code analyzers are not added: the SDK pinned in global.json is older than",
        "7.0.100 and does not support GlobalPackageReference (Central Package Management).",
    ];

    /// <summary>
    /// Moves every package version out of <paramref name="upstreamContent"/>.
    /// </summary>
    /// <param name="upstreamContent">The distributed props content, versions already bumped if requested.</param>
    /// <param name="area">The mapping area (<c>root</c>, <c>src</c>, <c>test</c>, <c>sample</c>), used to label appended groups.</param>
    /// <param name="supportsGlobalPackageReference">Whether the building SDK understands <c>GlobalPackageReference</c>.</param>
    public static CpmTransformResult Transform(
        string upstreamContent,
        string area,
        bool supportsGlobalPackageReference)
    {
        ArgumentNullException.ThrowIfNull(upstreamContent);

        var items = PackageItemParser.Parse(upstreamContent);
        if (items is null)
        {
            return new CpmTransformResult(upstreamContent, [], AnalyzersSkipped: false);
        }

        var content = upstreamContent;
        var entries = new List<CpmRequiredEntry>();

        var analyzers = items
            .Where(x => IsVersionedReference(x) &&
                        string.Equals(x.ItemGroupLabel, CodeAnalyzersLabel, StringComparison.Ordinal))
            .ToList();

        var analyzersSkipped = false;
        if (analyzers.Count > 0)
        {
            if (supportsGlobalPackageReference)
            {
                entries.AddRange(analyzers.Select(x => new CpmRequiredEntry(
                    PackageItemType.GlobalPackageReference,
                    x.Id,
                    x.Version!,
                    GroupLabelPrefix + CodeAnalyzersLabel)));
            }
            else
            {
                analyzersSkipped = true;
            }

            content = MsBuildFileTextEditor.ReplaceItemGroup(
                content,
                CodeAnalyzersLabel,
                supportsGlobalPackageReference ? AnalyzersMovedComment : AnalyzersSkippedComment);
        }

        // Each edit shifts later elements, so re-parse and handle one reference at a time.
        while (NextVersionedReference(content) is { } reference)
        {
            entries.Add(new CpmRequiredEntry(
                PackageItemType.PackageVersion,
                reference.Id,
                reference.Version!,
                GroupLabelPrefix + area));

            content = MsBuildFileTextEditor.RemoveVersion(content, reference);
        }

        return new CpmTransformResult(content, entries, analyzersSkipped);
    }

    private static PackageItem? NextVersionedReference(string content)
        => PackageItemParser.Parse(content)?.FirstOrDefault(IsVersionedReference);

    private static bool IsVersionedReference(PackageItem item)
        => item.ItemType == PackageItemType.PackageReference &&
           item.Version is not null &&
           !item.IsImplicitlyDefined &&
           !item.IsUpdate;
}