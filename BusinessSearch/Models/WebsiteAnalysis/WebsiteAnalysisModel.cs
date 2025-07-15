using System.Collections.Generic;

namespace BusinessSearch.Models.WebsiteAnalysis
{
    public class WebsiteAnalysisModel
    {
        public ResponsivenessResult? ResponsivenessResult { get; set; }
        public GdprComplianceResult? GdprComplianceResult { get; set; }
        public PageSpeedResult? PageSpeedResult { get; set; }
        public AccessibilityAnalysisResult? AccessibilityResult { get; set; }
        public LocalSeoAnalysisResult? LocalSeoResult { get; set; }
    }

    public class ResponsivenessResult
    {
        public bool IsResponsive { get; set; }
        public int Score { get; set; }
        public List<string> Details { get; set; } = new();
    }

    public class GdprComplianceResult
    {
        public bool HasCookieConsent { get; set; }
        public bool HasPrivacyPolicy { get; set; }
        public List<string> ComplianceIndicators { get; set; } = new();
    }
}