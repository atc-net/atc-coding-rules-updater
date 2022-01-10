// ReSharper disable InvertIf
// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
namespace Atc.CodingRules.Updater;

public static class DirectoryBuildPropsHelper
{
    public const string FileNameDirectoryBuildProps = "Directory.Build.props";

    public static void HandleFile(
        ILogger logger,
        string area,
        string rawCodingRulesDistribution,
        bool useLatestMinorNugetVersion,
        DirectoryInfo path,
        string urlPart)
    {
        ArgumentNullException.ThrowIfNull(path);

        var descriptionPart = string.IsNullOrEmpty(urlPart)
            ? $"[yellow]/[/]{FileNameDirectoryBuildProps}"
            : $"[yellow]{urlPart}/[/]{FileNameDirectoryBuildProps}";

        var file = new FileInfo(Path.Combine(path.FullName, FileNameDirectoryBuildProps));

        var rawGitUrl = string.IsNullOrEmpty(urlPart)
            ? $"{rawCodingRulesDistribution}/{FileNameDirectoryBuildProps}"
            : $"{rawCodingRulesDistribution}/{urlPart}/{FileNameDirectoryBuildProps}";

        try
        {
            if (!file.Directory!.Exists)
            {
                Directory.CreateDirectory(file.Directory.FullName);
            }

            var contentGit = HttpClientHelper.GetAsString(logger, rawGitUrl);
            if (useLatestMinorNugetVersion)
            {
                contentGit = EnsureLatestPackageReferencesVersion(logger, contentGit, LogCategoryType.Trace);
            }

            if (!file.Exists)
            {
                FileHelper.CreateFile(logger, file, contentGit, descriptionPart);
                return;
            }

            var contentFile = FileHelper.ReadAllText(file);
            if (string.IsNullOrEmpty(contentFile))
            {
                FileHelper.CreateFile(logger, file, contentGit, descriptionPart);
                return;
            }

            if (FileHelper.IsFileDataLengthEqual(contentGit, contentFile) &&
                contentGit.Equals(contentFile, StringComparison.Ordinal))
            {
                logger.LogInformation($"{EmojisConstants.FileNotUpdated}    {descriptionPart} nothing to update");
                return;
            }

            UpdateFile(logger, file, contentFile, descriptionPart, useLatestMinorNugetVersion);
        }
        catch (Exception ex)
        {
            logger.LogError($"{EmojisConstants.Error} {area} - {ex.Message}");
        }
    }

    public static bool HasFileInsertPlaceholderElement(DirectoryInfo path, string elementName, string elementValue)
    {
        var file = new FileInfo(Path.Combine(path.FullName, FileNameDirectoryBuildProps));
        if (!file.Exists)
        {
            return false;
        }

        var fileContent = FileHelper.ReadAllText(file);
        var searchText = $"<{elementName}><!-- {elementValue} --></{elementName}>";
        return fileContent.IndexOf(searchText, StringComparison.Ordinal) != -1;
    }

    public static void UpdateFileInsertPlaceholderElement(
        ILogger logger,
        DirectoryInfo path,
        string elementName,
        string elementValue,
        string newElementValue)
    {
        var file = new FileInfo(Path.Combine(path.FullName, FileNameDirectoryBuildProps));
        if (!file.Exists)
        {
            return;
        }

        var fileContent = FileHelper.ReadAllText(file);
        var searchText = $"<{elementName}><!-- {elementValue} --></{elementName}>";
        if (fileContent.IndexOf(searchText, StringComparison.Ordinal) != -1)
        {
            fileContent = fileContent.Replace(
                searchText,
                $"<{elementName}>{newElementValue}</{elementName}>",
                StringComparison.Ordinal);

            File.WriteAllText(file.FullName, fileContent);
            logger.LogDebug($"{EmojisConstants.FileUpdated}   {elementName} in file is updated to '{newElementValue}'");
        }
    }

    private static void UpdateFile(
        ILogger logger,
        FileInfo file,
        string fileContent,
        string descriptionPart,
        bool useLatestMinorNugetVersion)
    {
        if (useLatestMinorNugetVersion)
        {
            var newFileContent = EnsureLatestPackageReferencesVersion(logger, fileContent, LogCategoryType.Debug);
            if (!FileHelper.IsFileDataLengthEqual(fileContent, newFileContent) ||
                !fileContent.Equals(newFileContent, StringComparison.Ordinal))
            {
                fileContent = newFileContent;
            }
        }

        File.WriteAllText(file.FullName, fileContent);
        logger.LogDebug($"{EmojisConstants.FileUpdated}   {descriptionPart} updated");
    }

    private static string EnsureLatestPackageReferencesVersion(
        ILogger logger,
        string fileContent,
        LogCategoryType logCategoryType)
    {
        var packageReferencesThatNeedsToBeUpdated = GetPackageReferencesThatNeedsToBeUpdated(logger, fileContent);
        foreach (var item in packageReferencesThatNeedsToBeUpdated)
        {
            fileContent = fileContent.Replace(
                $"<PackageReference Include=\"{item.PackageId}\" Version=\"{item.Version}\"",
                $"<PackageReference Include=\"{item.PackageId}\" Version=\"{item.NewestVersion}\"",
                StringComparison.Ordinal);

            var logMessage = $"{EmojisConstants.PackageReference}   PackageReference {item.PackageId} @ {item.Version} => {item.NewestVersion}";
            switch (logCategoryType)
            {
                case LogCategoryType.Debug:
                    logger.LogDebug(logMessage);
                    break;
                case LogCategoryType.Trace:
                    logger.LogTrace(logMessage);
                    break;
                default:
                    throw new SwitchCaseDefaultException(logCategoryType);
            }
        }

        return fileContent;
    }

    private static List<DotnetNugetPackage> GetPackageReferencesThatNeedsToBeUpdated(
        ILogger logger,
        string fileContent)
    {
        var result = new List<DotnetNugetPackage>();

        var packageReferencesGit = DotnetNugetHelper.GetAllPackageReferences(fileContent);
        if (packageReferencesGit.Any())
        {
            foreach (var item in packageReferencesGit)
            {
                if (Version.TryParse(item.Version, out var version))
                {
                    var latestVersion = AtcApiNugetClientHelper.GetLatestVersionForPackageId(logger, item.PackageId, CancellationToken.None);

                    // TODO: Change GreaterThan to GreaterThan-Without-Major
                    if (latestVersion is not null &&
                        latestVersion.GreaterThan(version))
                    {
                        result.Add(
                            new DotnetNugetPackage(
                                item.PackageId,
                                version,
                                latestVersion));
                    }
                }
            }
        }

        return result;
    }
}