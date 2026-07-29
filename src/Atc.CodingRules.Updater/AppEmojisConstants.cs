namespace Atc.CodingRules.Updater;

/// <summary>
/// Updater-specific emoji constants. Lives alongside <c>Atc.Helpers.EmojisConstants</c>
/// (shared cross-tool icons like <c>Error</c>, <c>Success</c>, <c>FileCreated/Updated/NotUpdated</c>);
/// this class adds the icons that only make sense inside the updater
/// (per-area headers and per-event markers).
/// </summary>
public static class AppEmojisConstants
{
    /// <summary>Header icon for the editor-config processing phase.</summary>
    public const string AreaEditorConfig = Emoji.Known.MouseFace;

    /// <summary>Header icon for the Directory.Build.props processing phase.</summary>
    public const string AreaDirectoryBuildProps = Emoji.Known.Hammer;

    /// <summary>Header icon for the temporary-suppression build/loop phase.</summary>
    public const string AreaTemporarySuppression = Emoji.Known.CardIndex;

    /// <summary>Marker for log lines that announce a NuGet package version bump.</summary>
    public const string PackageReference = Emoji.Known.Package;

    /// <summary>Marker for warnings about duplicate dotnet_diagnostic.* keys between git and custom sections.</summary>
    public const string DuplicateKey = Emoji.Known.Key;

    /// <summary>Marker for lines reporting Directory.Build.props drift against the distribution.</summary>
    public const string Drift = Emoji.Known.Compass;
}