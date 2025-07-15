namespace BusinessSearch.Models.WebsiteAnalysis
{
    public enum WcagLevel
    {
        A,
        AA,
        AAA
    }

    public class AccessibilityIssue
    {
        public string Criterion { get; set; }
        public WcagLevel Level { get; set; }
        public string Description { get; set; }
        public string Element { get; set; }
        public string Recommendation { get; set; }
        public string Impact { get; set; }
    }

    public class AccessibilityAnalysisResult
    {
        public bool HasIssues => Issues.Count > 0;
        public List<AccessibilityIssue> Issues { get; set; } = new List<AccessibilityIssue>();
        public Dictionary<WcagLevel, int> IssueCountByLevel { get; set; } = new Dictionary<WcagLevel, int>();
        public int TotalIssues => Issues.Count;
        public double ComplianceScore { get; set; }
        public string PageUrl { get; set; }
        public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;
    }

    public class ContrastCheckResult
    {
        public string ForegroundColor { get; set; }
        public string BackgroundColor { get; set; }
        public double ContrastRatio { get; set; }
        public bool PassesAA { get; set; }
        public bool PassesAAA { get; set; }
    }
}
