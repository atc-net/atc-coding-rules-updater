using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace Atc.CodingRules.Updater.CLI
{
    public static class HttpClientHelper
    {
        private static readonly ConcurrentDictionary<string, string> Cache = new ConcurrentDictionary<string, string>(System.StringComparer.Ordinal);

        [SuppressMessage("Design", "CA1054:URI-like parameters should not be strings", Justification = "OK.")]
        public static string GetRawFile(string rawFileUrl)
        {
            using var client = new WebClient();
            return Cache.GetOrAdd(rawFileUrl, client.DownloadString(rawFileUrl));
        }
    }
}