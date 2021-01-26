using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Atc.Data.Models;

// ReSharper disable InvertIf
namespace Atc.CodingRules.Updater.CLI
{
    public static class DirectoryBuildPropsHelper
    {
        public const string FileNameDirectoryBuildProps = "Directory.Build.props";

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "OK.")]
        [SuppressMessage("Design", "CA1054:URI-like parameters should not be strings", Justification = "OK.")]
        public static IEnumerable<LogKeyValueItem> HandleFile(
            bool isFirstTime,
            string area,
            string rawCodingRulesDistribution,
            DirectoryInfo path,
            string urlPart)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            var descriptionPart = string.IsNullOrEmpty(urlPart)
                ? FileNameDirectoryBuildProps
                : $"{urlPart}/{FileNameDirectoryBuildProps}";

            var file = new FileInfo(Path.Combine(path.FullName, FileNameDirectoryBuildProps));

            var rawGitUrl = string.IsNullOrEmpty(urlPart)
                ? $"{rawCodingRulesDistribution}/{FileNameDirectoryBuildProps}"
                : $"{rawCodingRulesDistribution}/{urlPart}/{FileNameDirectoryBuildProps}";

            var logItems = new List<LogKeyValueItem>();
            try
            {
                if (!file.Directory!.Exists)
                {
                    if (!isFirstTime)
                    {
                        logItems.Add(new LogKeyValueItem(LogCategoryType.Trace, "FileSkip", $"{descriptionPart} skipped"));
                        return logItems;
                    }

                    Directory.CreateDirectory(file.Directory.FullName);
                }

                var rawGitData = HttpClientHelper.GetRawFile(rawGitUrl);
                var rawFileData = FileHelper.ReadAllText(file);

                if (FileHelper.IsFileDataLengthEqual(rawGitData, rawFileData))
                {
                    logItems.Add(new LogKeyValueItem(LogCategoryType.Debug, "FileSkip", $"{descriptionPart} skipped"));
                    return logItems;
                }

                if (string.IsNullOrEmpty(rawFileData))
                {
                    return CreateFile(file, rawGitData, logItems, descriptionPart);
                }

                return UpdateFile(file, rawGitData, logItems, descriptionPart);
            }
            catch (Exception ex)
            {
                logItems.Add(new LogKeyValueItem(LogCategoryType.Error, "FileSkip", $"{area} skipped - {ex.Message}"));
                return logItems;
            }
        }

        private static IEnumerable<LogKeyValueItem> CreateFile(
            FileInfo file,
            string rawGitData,
            List<LogKeyValueItem> logItems,
            string descriptionPart)
        {
            File.WriteAllText(file.FullName, rawGitData);
            logItems.Add(new LogKeyValueItem(LogCategoryType.Debug, "FileCreate", $"{descriptionPart} created"));
            return logItems;
        }

        [SuppressMessage("Major Code Smell", "S1172:Unused method parameters should be removed", Justification = "OK for now.")]
        [SuppressMessage("Info Code Smell", "S1135:Track uses of \"TODO\" tags", Justification = "OK for now.")]
        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "OK for now.")]
        [SuppressMessage("Usage", "CA1801:Review unused parameters", Justification = "OK for now.")]
        private static IEnumerable<LogKeyValueItem> UpdateFile(
            FileInfo file,
            string rawGitData,
            List<LogKeyValueItem> logItems,
            string descriptionPart)
        {
            // TODO: Handle XML
            ////File.WriteAllText(file.FullName, rawGitData);
            ////logItems.Add(new LogKeyValueItem(LogCategoryType.Debug, "UpdateCreate", $"{descriptionPart} updated"));
            return logItems;
        }
    }
}