using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessSearch.Services
{
    public interface IWebsiteAnalysisService
    {
        Task<WebsiteAnalysisResult> AnalyzeWebsite(string url);
    }

    public class WebsiteAnalysisResult
    {
        public ResponsivenessResult? ResponsivenessResult { get; set; }
        public GdprComplianceResult? GdprComplianceResult { get; set; }
    }

    public class ResponsivenessResult
    {
        public bool IsResponsive { get; set; }
        public List<string>? Details { get; set; }
        public int Score { get; set; }
    }

    public class GdprComplianceResult
    {
        public bool HasCookieConsent { get; set; }
        public bool HasPrivacyPolicy { get; set; }
        public List<string>? OtherComplianceIndicators { get; set; }
    }
}
