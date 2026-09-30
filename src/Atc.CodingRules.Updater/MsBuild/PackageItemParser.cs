namespace Atc.CodingRules.Updater.MsBuild;

/// <summary>
/// Reads package items from MSBuild file content.
/// </summary>
/// <remarks>
/// Elements are matched on their local name, so a file carrying the legacy MSBuild namespace is
/// handled the same as one without it.
/// </remarks>
public static class PackageItemParser
{
    /// <summary>
    /// Returns every package item in <paramref name="content"/>, in document order, or
    /// <c>null</c> when the content is not parseable XML.
    /// </summary>
    public static IReadOnlyList<PackageItem>? Parse(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        XDocument document;
        try
        {
            document = XDocument.Parse(content, LoadOptions.SetLineInfo);
        }
        catch (XmlException)
        {
            return null;
        }

        var result = new List<PackageItem>();
        foreach (var element in document.Descendants())
        {
            if (!Enum.TryParse<PackageItemType>(element.Name.LocalName, ignoreCase: false, out var itemType))
            {
                continue;
            }

            var include = element.Attribute("Include")?.Value;
            var update = element.Attribute("Update")?.Value;
            var id = include ?? update;
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            var versionAttribute = element.Attribute("Version")?.Value;
            var versionElement = ChildElement(element, "Version")?.Value;
            var itemGroup = element.Parent;

            result.Add(new PackageItem(
                itemType,
                id,
                versionAttribute ?? versionElement,
                IsVersionChildElement: versionAttribute is null && versionElement is not null,
                VersionOverride: element.Attribute("VersionOverride")?.Value ?? ChildElement(element, "VersionOverride")?.Value,
                IsUpdate: include is null,
                IsConditional: element.Attribute("Condition") is not null || itemGroup?.Attribute("Condition") is not null,
                IsImplicitlyDefined: string.Equals(element.Attribute("IsImplicitlyDefined")?.Value, "true", StringComparison.OrdinalIgnoreCase),
                ItemGroupLabel: itemGroup?.Attribute("Label")?.Value,
                LineNumber: ((IXmlLineInfo)element).LineNumber));
        }

        return result;
    }

    private static XElement? ChildElement(
        XElement element,
        string localName)
        => element
            .Elements()
            .FirstOrDefault(x => x.Name.LocalName.Equals(localName, StringComparison.Ordinal));
}