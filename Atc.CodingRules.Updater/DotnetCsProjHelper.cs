namespace Atc.CodingRules.Updater
{
    public static class DotnetCsProjHelper
    {
        public static List<FileInfo> SearchAllForElement(
            DirectoryInfo projectPath,
            string elementName,
            string? elementValue = null)
        {
            var result = new List<FileInfo>();
            var files = Directory.GetFiles(projectPath.FullName, "*.csproj");
            foreach (var file in files)
            {
                var fileContent = File.ReadAllText(file);
                var searchText = $"<{elementName}";
                if (elementValue is not null)
                {
                    searchText = $"<{elementName}>{elementValue}</{elementName}>";
                }

                if (fileContent.IndexOf(searchText, StringComparison.Ordinal) != -1)
                {
                    result.Add(new FileInfo(file));
                }
            }

            return result;
        }
    }
}