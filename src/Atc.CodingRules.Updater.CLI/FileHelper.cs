using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Atc.CodingRules.Updater.CLI
{
    public static class FileHelper
    {
        public static readonly string[] LineBreaks = { "\r\n", "\r", "\n" };

        public static string ReadAllText(FileInfo file)
        {
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            return file.Exists
                ? File.ReadAllText(file.FullName)
                : string.Empty;
        }

        public static void CreateFile(
            ILogger logger,
            FileInfo file,
            string rawGitData,
            string descriptionPart)
        {
            File.WriteAllText(file.FullName, rawGitData);
            logger.LogInformation($"{EmojisConstants.FileCreated}    {descriptionPart} created");
        }

        public static bool IsFileDataLengthEqual(string dataA, string dataB)
        {
            if (dataA is null)
            {
                throw new ArgumentNullException(nameof(dataA));
            }

            if (dataB is null)
            {
                throw new ArgumentNullException(nameof(dataB));
            }

            var l1 = dataA
                .Replace("\r\n", string.Empty, StringComparison.Ordinal)
                .Replace("\r", string.Empty, StringComparison.Ordinal)
                .Replace("\n", string.Empty, StringComparison.Ordinal)
                .Length;

            var l2 = dataB
                .Replace("\r\n", string.Empty, StringComparison.Ordinal)
                .Replace("\r", string.Empty, StringComparison.Ordinal)
                .Replace("\n", string.Empty, StringComparison.Ordinal)
                .Length;

            return l1.Equals(l2);
        }
    }
}