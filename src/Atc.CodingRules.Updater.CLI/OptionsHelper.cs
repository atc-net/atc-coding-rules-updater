using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Atc.CodingRules.Updater.CLI.Models;
using McMaster.Extensions.CommandLineUtils;

namespace Atc.CodingRules.Updater.CLI
{
    public static class OptionsHelper
    {
        public static OptionRoot CreateDefault(CommandLineApplication configCmd)
        {
            if (configCmd is null)
            {
                throw new ArgumentNullException(nameof(configCmd));
            }

            var cmdOptionOptionsPath = configCmd
                .GetOptions()
                .FirstOrDefault(x => x.LongName!.Equals("optionsPath", StringComparison.OrdinalIgnoreCase));

            string optionsPath;
            if (cmdOptionOptionsPath is null || string.IsNullOrEmpty(cmdOptionOptionsPath.Value()))
            {
                optionsPath = CommandLineApplicationHelper.GetRootPath(configCmd).FullName;
            }
            else
            {
                optionsPath = cmdOptionOptionsPath.Value()!;
            }

            var fileInfo = optionsPath.EndsWith(".json", StringComparison.Ordinal)
                ? new FileInfo(optionsPath)
                : new FileInfo(Path.Combine(optionsPath, "atc-coding-rules-updater.json"));

            var options = DeserializeFile(fileInfo);
            options.Mappings.ResolvePaths(new DirectoryInfo(optionsPath));
            return options;
        }

        private static OptionRoot DeserializeFile(FileInfo fileInfo)
        {
            var options = new OptionRoot();

            if (!fileInfo.Exists)
            {
                return options;
            }

            var serializeOptions = new JsonSerializerOptions();
            serializeOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            serializeOptions.WriteIndented = true;

            using var stream = new StreamReader(fileInfo.FullName);
            var json = stream.ReadToEnd();
            options = JsonSerializer.Deserialize<OptionRoot>(json, serializeOptions);

            return options;
        }
    }
}