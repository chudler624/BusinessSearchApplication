using System.Threading.Tasks;

namespace BusinessSearch.Services.WebsiteOpportunitiesServices
{
    public interface IWebsiteOpportunitiesService
    {
        Task<WebsiteAnalysisResult> AnalyzeWebsite(string url);
    }

    public class WebsiteAnalysisResult
    {
        public ResponsivenessResult? ResponsivenessResult { get; set; }
        public GdprComplianceResult? GdprComplianceResult { get; set; }
    }
}
