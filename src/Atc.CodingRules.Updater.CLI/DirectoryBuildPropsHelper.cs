using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.Extensions.Logging;

// ReSharper disable InvertIf
namespace Atc.CodingRules.Updater.CLI
{
    public static class DirectoryBuildPropsHelper
    {
        public const string FileNameDirectoryBuildProps = "Directory.Build.props";

        public static void HandleFile(
            ILogger logger,
            bool isFirstTime,
            string area,
            string rawCodingRulesDistribution,
            DirectoryInfo path,
            string urlPart)
        {
            if (path is null)
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

            try
            {
                if (!file.Directory!.Exists)
                {
                    if (!isFirstTime)
                    {
                        logger.LogInformation($"{EmojisConstants.FileNotUpdated}    {descriptionPart} nothing to update");
                        return;
                    }

                    Directory.CreateDirectory(file.Directory.FullName);
                }

                var rawGitData = HttpClientHelper.GetRawFile(rawGitUrl);
                var rawFileData = FileHelper.ReadAllText(file);

                if (FileHelper.IsFileDataLengthEqual(rawGitData, rawFileData))
                {
                    logger.LogInformation($"{EmojisConstants.FileNotUpdated}    {descriptionPart} nothing to update");
                    return;
                }

                if (string.IsNullOrEmpty(rawFileData))
                {
                    FileHelper.CreateFile(logger, file, rawGitData, descriptionPart);
                }
                else
                {
                    UpdateFile(logger, file, rawGitData, descriptionPart);
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"{EmojisConstants.Error} {area} - {ex.Message}");
            }
        }

        [SuppressMessage("Info Code Smell", "S1135:Track uses of \"TODO\" tags", Justification = "OK for now.")]
        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "OK for now.")]
        private static void UpdateFile(
            ILogger logger,
            FileInfo file,
            string rawGitData,
            string descriptionPart)
        {
            // TODO: Handle XML
            ////File.WriteAllText(file.FullName, rawGitData);
            ////logger.LogDebug($"{EmojisConstants.FileUpdated}   {descriptionPart} updated");
        }
    }
}