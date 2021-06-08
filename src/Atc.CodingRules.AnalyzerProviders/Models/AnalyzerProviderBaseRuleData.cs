using System.Collections.Generic;

namespace Atc.CodingRules.AnalyzerProviders.Models
{
    public class AnalyzerProviderBaseRuleData
    {
        public AnalyzerProviderBaseRuleData()
        {
        }

        public AnalyzerProviderBaseRuleData(string name)
        {
            this.Name = name;
        }

        public string Name { get; set; } = string.Empty;

        public ICollection<Rule> Rules { get; set; } = new List<Rule>();

        public override string ToString() => $"{nameof(Name)}: {Name}, {nameof(Rules)}.Count: {Rules?.Count}";
    }
}