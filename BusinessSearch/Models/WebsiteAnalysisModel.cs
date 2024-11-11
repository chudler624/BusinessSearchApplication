using System.Collections.Generic;

namespace BusinessSearch.Models.WebsiteAnalysis
{
    public class WebsiteAnalysisResult
    {
        public ResponsivenessResult? ResponsivenessResult { get; set; }
        public GdprComplianceResult? GdprComplianceResult { get; set; }
        public PageSpeedResult? PageSpeedResult { get; set; }
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

    public class PageSpeedResult
    {
        public double Score { get; set; }
        public bool IsFast { get; set; }
        public CoreWebVitals? WebVitals { get; set; }
        public ResourceMetrics? ResourceMetrics { get; set; }
        public List<string> Details { get; set; } = new List<string>();
    }

    public class CoreWebVitals
    {
        public double LargestContentfulPaint { get; set; }
        public double FirstInputDelay { get; set; }
        public double TimeToFirstByte { get; set; }
        public double CumulativeLayoutShift { get; set; }
    }

    public class ResourceMetrics
    {
        public long TotalPageSize { get; set; }
        public int TotalRequests { get; set; }
        public Dictionary<string, int> ResourceCounts { get; set; } = new Dictionary<string, int>();
    }
}