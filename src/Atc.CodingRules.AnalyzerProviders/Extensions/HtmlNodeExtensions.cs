// ReSharper disable once CheckNamespace
namespace HtmlAgilityPack;

public static class HtmlNodeExtensions
{
    public static bool HasTitleWithAccessDenied(this HtmlNode htmlNode)
    {
        ArgumentNullException.ThrowIfNull(htmlNode);

        var titleText = htmlNode.SelectSingleNode("//*//title")?.InnerText;
        return titleText is not null &&
               titleText.Contains("Access", StringComparison.OrdinalIgnoreCase) &&
               titleText.Contains("Denied", StringComparison.OrdinalIgnoreCase);
    }
}