using System;
using Atc.CodingRules.AnalyzerRulesMetaData.Generator.CLI.Models;

namespace Atc.CodingRules.AnalyzerRulesMetaData.Generator.CLI.AnalyzerProviders
{
    public abstract class AnalyzerProviderBase : IAnalyzerProvider
    {
        public virtual Uri? DocumentationLink { get; set; }

        public virtual AnalyzerProviderData RetrieveData()
        {
            throw new NotImplementedException("Please override RetrieveData.");
        }
    }
}