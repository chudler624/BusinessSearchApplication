using BusinessSearch.Models.WebsiteAnalysis;

namespace BusinessSearch.Services.WebsiteOpportunitiesServices
{
    public interface IGdprComplianceService
    {
        GdprComplianceResult AnalyzeGdprCompliance(string content);
    }
}