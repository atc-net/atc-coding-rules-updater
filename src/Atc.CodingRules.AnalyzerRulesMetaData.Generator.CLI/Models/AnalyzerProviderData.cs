using System.Collections.Generic;

namespace Atc.CodingRules.AnalyzerRulesMetaData.Generator.CLI.Models
{
    public class AnalyzerProviderData
    {
        public AnalyzerProviderData()
        {
        }

        public AnalyzerProviderData(string name)
        {
            this.Name = name;
        }

        public string Name { get; set; } = string.Empty;

        public ICollection<Rule> Rules { get; set; } = new List<Rule>();

        public override string ToString() => $"{nameof(Name)}: {Name}, {nameof(Rules)}.Count: {Rules?.Count}";
    }
}