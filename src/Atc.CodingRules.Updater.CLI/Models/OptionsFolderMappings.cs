using System.Collections.Generic;

namespace Atc.CodingRules.Updater.CLI.Models
{
    public class OptionsFolderMappings
    {
        public IList<string> Paths { get; set; } = new List<string>();

        public override string ToString() => $"{nameof(Paths)}.Count: {Paths?.Count}";
    }
}