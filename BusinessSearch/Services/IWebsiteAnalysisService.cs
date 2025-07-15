using BusinessSearch.Models.WebsiteAnalysis;

namespace BusinessSearch.Services
{
    public interface IWebsiteAnalysisService
    {
        Task<WebsiteAnalysisModel> AnalyzeWebsite(string url);
    }
}