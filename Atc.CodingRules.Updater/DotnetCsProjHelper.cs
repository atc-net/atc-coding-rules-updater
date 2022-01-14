namespace Atc.CodingRules.Updater
{
    public static class DotnetCsProjHelper
    {
        public static Collection<FileInfo> SearchAllForElement(
            DirectoryInfo projectPath,
            string elementName,
            string? elementValue = null,
            SearchOption searchOption = SearchOption.AllDirectories,
            StringComparison stringComparison = StringComparison.Ordinal)
            => FileHelper.SearchAllForElement(
                projectPath,
                "*.csproj",
                elementName,
                elementValue,
                searchOption,
                stringComparison);
    }
}