using BusinessSearch.Models.WebsiteAnalysis;

namespace BusinessSearch.Services.WebsiteOpportunitiesServices.Interfaces
{
    public interface ILocalSeoService
    {
        Task<LocalSeoAnalysisResult> AnalyzeLocalSeo(string content);
    }
}
