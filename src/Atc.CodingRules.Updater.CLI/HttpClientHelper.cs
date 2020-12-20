using System.Collections.Concurrent;
using System.Net;

namespace Atc.CodingRules.Updater.CLI
{
    public static class HttpClientHelper
    {
        private static readonly ConcurrentDictionary<string, string> Cache = new ConcurrentDictionary<string, string>(System.StringComparer.Ordinal);

        public static string GetRawFile(string rawFileUrl)
        {
            using var client = new WebClient();
            return Cache.GetOrAdd(rawFileUrl, client.DownloadString(rawFileUrl));
        }
    }
}