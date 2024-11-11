using BusinessSearch.Models.WebsiteAnalysis;

namespace BusinessSearch.Services.WebsiteOpportunitiesServices
{
    public interface IResponsivenessService
    {
        ResponsivenessResult AnalyzeResponsiveness(string content);
    }
}