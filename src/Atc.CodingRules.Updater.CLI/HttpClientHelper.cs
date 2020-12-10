using System.Net;

namespace Atc.CodingRules.Updater.CLI
{
    public static class HttpClientHelper
    {
        public static string GetRawFile(string rawFileUrl)
        {
            using var client = new WebClient();
            return client.DownloadString(rawFileUrl);
        }
    }
}