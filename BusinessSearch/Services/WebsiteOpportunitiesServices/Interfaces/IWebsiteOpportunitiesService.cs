using BusinessSearch.Models.WebsiteAnalysis;

namespace BusinessSearch.Services.WebsiteOpportunitiesServices.Interfaces
{
    public interface IWebsiteOpportunitiesService
    {
        Task<WebsiteAnalysisModel> AnalyzeWebsite(string url);
    }
}