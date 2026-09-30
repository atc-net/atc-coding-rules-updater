namespace Atc.CodingRules.Updater.MsBuild;

/// <summary>
/// One package item read from an MSBuild file by <see cref="PackageItemParser"/>.
/// </summary>
/// <param name="ItemType">Which item type the element is.</param>
/// <param name="Id">The package id, from <c>Include</c> (or <c>Update</c> when <see cref="IsUpdate"/>).</param>
/// <param name="Version">The version from the <c>Version</c> attribute or child element, or <c>null</c> when absent.</param>
/// <param name="IsVersionChildElement"><c>true</c> when the version is a <c>&lt;Version&gt;</c> child element rather than an attribute.</param>
/// <param name="VersionOverride">The <c>VersionOverride</c> value, or <c>null</c> when absent.</param>
/// <param name="IsUpdate"><c>true</c> when the element uses <c>Update</c> instead of <c>Include</c>.</param>
/// <param name="IsConditional"><c>true</c> when the element or its <c>ItemGroup</c> carries a <c>Condition</c>.</param>
/// <param name="IsImplicitlyDefined"><c>true</c> when the element sets <c>IsImplicitlyDefined="true"</c>.</param>
/// <param name="ItemGroupLabel">The <c>Label</c> of the containing <c>ItemGroup</c>, or <c>null</c>.</param>
/// <param name="LineNumber">1-based line of the element's start tag.</param>
public sealed record PackageItem(
    PackageItemType ItemType,
    string Id,
    string? Version,
    bool IsVersionChildElement,
    string? VersionOverride,
    bool IsUpdate,
    bool IsConditional,
    bool IsImplicitlyDefined,
    string? ItemGroupLabel,
    int LineNumber);