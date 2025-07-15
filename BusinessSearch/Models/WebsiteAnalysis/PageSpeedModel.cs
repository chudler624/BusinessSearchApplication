namespace BusinessSearch.Models.WebsiteAnalysis
{
    public class PageSpeedResult
    {
        public double Score { get; set; }
        public bool IsFast { get; set; }
        public WebVitals WebVitals { get; set; } = new();
        public ResourceMetrics ResourceMetrics { get; set; } = new();
        public List<string> Details { get; set; } = new();
    }

    public class WebVitals
    {
        public double TimeToFirstByte { get; set; }
        public double LargestContentfulPaint { get; set; }
        public double CumulativeLayoutShift { get; set; }
    }

    public class ResourceMetrics
    {
        public int TotalRequests { get; set; }
        public long TotalPageSize { get; set; }
        public Dictionary<string, int> ResourceCounts { get; set; } = new();
    }
}
