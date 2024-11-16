using BusinessSearch.Models.WebsiteAnalysis;

namespace BusinessSearch.Services.WebsiteOpportunitiesServices.Interfaces
{
    public interface IGdprComplianceService
    {
        GdprComplianceResult AnalyzeGdprCompliance(string content);
    }
}