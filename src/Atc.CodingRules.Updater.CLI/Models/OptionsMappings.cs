using System;
using System.IO;

namespace Atc.CodingRules.Updater.CLI.Models
{
    public class OptionsMappings
    {
        public OptionsFolderMappings Sample { get; set; } = new OptionsFolderMappings();

        public OptionsFolderMappings Src { get; set; } = new OptionsFolderMappings();

        public OptionsFolderMappings Test { get; set; } = new OptionsFolderMappings();

        public bool HasMappingsPaths() =>
            Sample?.Paths?.Count > 0 ||
            Src?.Paths?.Count > 0 ||
            Test?.Paths?.Count > 0;

        public void ResolvePaths(DirectoryInfo rootPath)
        {
            if (rootPath == null)
            {
                throw new ArgumentNullException(nameof(rootPath));
            }

            if (!HasMappingsPaths())
            {
                return;
            }

            for (int i = 0; i < Sample.Paths.Count; i++)
            {
                if (TryResolvePathIfNeeded(rootPath, Sample.Paths[i], out string newPath))
                {
                    Sample.Paths[i] = newPath;
                }
            }

            for (int i = 0; i < Src.Paths.Count; i++)
            {
                if (TryResolvePathIfNeeded(rootPath, Src.Paths[i], out string newPath))
                {
                    Src.Paths[i] = newPath;
                }
            }

            for (int i = 0; i < Test.Paths.Count; i++)
            {
                if (TryResolvePathIfNeeded(rootPath, Test.Paths[i], out string newPath))
                {
                    Test.Paths[i] = newPath;
                }
            }
        }

        public override string ToString()
        {
            return $"{nameof(Sample)}: ({Sample}), {nameof(Src)}: ({Src}), {nameof(Test)}: ({Test})";
        }

        private static bool TryResolvePathIfNeeded(DirectoryInfo rootPath, string orgPath, out string newPath)
        {
            newPath = string.Empty;
            var di = new DirectoryInfo(orgPath);
            if (di.FullName.Contains("Atc.CodingRules.Updater.CLI", StringComparison.Ordinal))
            {
                if (orgPath.IndexOfAny(new[] { '.', '/', '\\' }) == -1)
                {
                    newPath = Path.Combine(rootPath.FullName, orgPath);
                    return true;
                }

                if (orgPath.StartsWith("./", StringComparison.Ordinal))
                {
                    var s = orgPath.Substring(2).Replace("/", "\\", StringComparison.Ordinal);
                    newPath = Path.Combine(rootPath.FullName, s);
                    return true;
                }

                if (orgPath.IndexOfAny(new[] { '/', '\\' }) != -1)
                {
                    var s = orgPath.Replace("/", "\\", StringComparison.Ordinal);
                    newPath = Path.Combine(rootPath.FullName, s);
                    return true;
                }
            }

            if (orgPath.StartsWith("\\", StringComparison.Ordinal))
            {
                var s = orgPath.Substring(1).Replace("/", "\\", StringComparison.Ordinal);
                newPath = Path.Combine(rootPath.FullName, s);
                return true;
            }

            return false;
        }
    }
}