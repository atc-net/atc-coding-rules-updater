using System;

// ReSharper disable once CheckNamespace
namespace HtmlAgilityPack
{
    public static class HtmlNodeExtensions
    {
        public static bool HasTitleWithAccessDenied(this HtmlNode hmlNode)
        {
            if (hmlNode is null)
            {
                throw new ArgumentNullException(nameof(hmlNode));
            }

            var titleText = hmlNode.SelectSingleNode("//*//title")?.InnerText;
            return titleText is not null &&
                   titleText.Contains("Access", StringComparison.OrdinalIgnoreCase) &&
                   titleText.Contains("Denied", StringComparison.OrdinalIgnoreCase);
        }
    }
}