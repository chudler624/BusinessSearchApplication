using BusinessSearch.Models.WebsiteAnalysis;

namespace BusinessSearch.Services.WebsiteOpportunitiesServices.Interfaces
{
    public interface IResponsivenessService
    {
        ResponsivenessResult AnalyzeResponsiveness(string content);
    }
}