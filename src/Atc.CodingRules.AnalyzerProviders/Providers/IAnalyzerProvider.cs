using System.Threading.Tasks;
using Atc.CodingRules.AnalyzerProviders.Models;

namespace Atc.CodingRules.AnalyzerProviders.Providers
{
    public interface IAnalyzerProvider
    {
        Task<AnalyzerProviderBaseRuleData> CollectBaseRules();
    }
}