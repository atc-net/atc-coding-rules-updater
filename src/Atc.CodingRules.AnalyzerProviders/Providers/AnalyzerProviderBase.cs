using System;
using Atc.CodingRules.AnalyzerProviders.Models;

namespace Atc.CodingRules.AnalyzerProviders.Providers
{
    public abstract class AnalyzerProviderBase : IAnalyzerProvider
    {
        public virtual Uri? DocumentationLink { get; set; }

        public virtual AnalyzerProviderBaseRuleData CollectBaseRules()
        {
            throw new NotImplementedException("Please override RetrieveData.");
        }
    }
}