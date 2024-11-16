namespace BusinessSearch.Models.WebsiteAnalysis
{
    public class LocalSeoAnalysisResult
    {
        public bool HasConsistentNap { get; set; }
        public NapAnalysis NapDetails { get; set; } = new();
        public SchemaMarkupAnalysis SchemaMarkup { get; set; } = new();
        public bool HasGoogleMapsEmbedded { get; set; }
        public LocationKeywordsAnalysis KeywordsAnalysis { get; set; } = new();
        public MetaAnalysis MetaInformation { get; set; } = new();
        public StructuredDataAnalysis StructuredData { get; set; } = new();
        public UrlAnalysis UrlStructure { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public int OverallScore { get; set; }
    }

    public class NapAnalysis
    {
        public string? BusinessName { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public bool IsNapConsistent { get; set; }
        public List<string> InconsistenciesFound { get; set; } = new();
        public List<string> LocationsFound { get; set; } = new();
    }

    public class SchemaMarkupAnalysis
    {
        public bool HasLocalBusinessSchema { get; set; }
        public bool IsSchemaValid { get; set; }
        public string? SchemaType { get; set; }
        public List<string> MissingRequiredProperties { get; set; } = new();
    }

    public class LocationKeywordsAnalysis
    {
        public List<string> FoundKeywords { get; set; } = new();
        public Dictionary<string, int> KeywordDensity { get; set; } = new();
        public bool HasSufficientLocationKeywords { get; set; }
    }

    public class MetaAnalysis
    {
        public bool HasLocationInTitle { get; set; }
        public bool HasLocationInDescription { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public List<string> Suggestions { get; set; } = new();
    }

    public class StructuredDataAnalysis
    {
        public bool HasStructuredData { get; set; }
        public List<string> ImplementedTypes { get; set; } = new();
        public bool IsValid { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
    }

    public class UrlAnalysis
    {
        public bool HasLocationInUrl { get; set; }
        public bool IsUrlOptimized { get; set; }
        public string? CurrentUrlStructure { get; set; }
        public List<string> OptimizationSuggestions { get; set; } = new();
    }
}
