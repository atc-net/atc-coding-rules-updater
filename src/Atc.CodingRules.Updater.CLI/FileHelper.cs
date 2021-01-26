using System;
using System.IO;

namespace Atc.CodingRules.Updater.CLI
{
    public static class FileHelper
    {
        public static readonly string[] LineBreaks = { "\r\n", "\r", "\n" };

        public static string ReadAllText(FileInfo file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            return file.Exists
                ? File.ReadAllText(file.FullName)
                : string.Empty;
        }

        public static bool IsFileDataLengthEqual(string dataA, string dataB)
        {
            if (dataA == null)
            {
                throw new ArgumentNullException(nameof(dataA));
            }

            if (dataB == null)
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