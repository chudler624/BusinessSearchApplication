using HtmlAgilityPack;
using System.Text.RegularExpressions;
using BusinessSearch.Services.WebsiteOpportunitiesServices.Interfaces;
using BusinessSearch.Models.WebsiteAnalysis;

namespace BusinessSearch.Services.WebsiteOpportunitiesServices
{
    public class LocalSeoService : ILocalSeoService
    {
        private readonly ILogger<LocalSeoService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public LocalSeoService(
            ILogger<LocalSeoService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<LocalSeoAnalysisResult> AnalyzeLocalSeo(string content)
        {
            try
            {
                var result = new LocalSeoAnalysisResult();

                // Execute all analyses
                result.NapDetails = await AnalyzeNapConsistency(content);
                result.SchemaMarkup = await AnalyzeSchemaMarkup(content);
                result.KeywordsAnalysis = await AnalyzeLocationKeywords(content);
                result.HasGoogleMapsEmbedded = await VerifyGoogleMapsPresence(content);
                result.MetaInformation = await AnalyzeMetaInformation(content);
                result.StructuredData = await AnalyzeStructuredData(content);
                result.UrlStructure = await AnalyzeUrlStructure(content);

                // Calculate overall score and generate recommendations
                result.OverallScore = CalculateOverallScore(result);
                result.Recommendations = GenerateRecommendations(result);
                result.HasConsistentNap = result.NapDetails.IsNapConsistent;

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Local SEO analysis");
                throw;
            }
        }

        public async Task<NapAnalysis> AnalyzeNapConsistency(string content)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(content);

            var result = new NapAnalysis
            {
                BusinessName = ExtractBusinessName(doc),
                Address = ExtractAddress(doc),
                Phone = ExtractPhone(doc)
            };

            result.IsNapConsistent = !string.IsNullOrEmpty(result.BusinessName) &&
                                   !string.IsNullOrEmpty(result.Address) &&
                                   !string.IsNullOrEmpty(result.Phone);

            return result;
        }

        public async Task<SchemaMarkupAnalysis> AnalyzeSchemaMarkup(string content)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(content);
            var result = new SchemaMarkupAnalysis();

            var schemaScripts = doc.DocumentNode.SelectNodes("//script[@type='application/ld+json']");
            result.HasLocalBusinessSchema = schemaScripts != null &&
                schemaScripts.Any(s => s.InnerText.Contains("LocalBusiness"));

            return result;
        }

        public async Task<LocationKeywordsAnalysis> AnalyzeLocationKeywords(string content)
        {
            var result = new LocationKeywordsAnalysis();
            var doc = new HtmlDocument();
            doc.LoadHtml(content);

            // Extract visible text
            var text = doc.DocumentNode.SelectNodes("//body//text()")
                ?.Select(node => node.InnerText.Trim())
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .ToList() ?? new List<string>();

            result.HasSufficientLocationKeywords = text.Any();
            return result;
        }

        public async Task<bool> VerifyGoogleMapsPresence(string content)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(content);

            return doc.DocumentNode.SelectNodes("//iframe[contains(@src, 'google.com/maps')]") != null;
        }

        public async Task<MetaAnalysis> AnalyzeMetaInformation(string content)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(content);

            var result = new MetaAnalysis
            {
                MetaTitle = doc.DocumentNode.SelectSingleNode("//title")?.InnerText,
                MetaDescription = doc.DocumentNode.SelectSingleNode("//meta[@name='description']")
                    ?.GetAttributeValue("content", null)
            };

            result.HasLocationInTitle = !string.IsNullOrEmpty(result.MetaTitle);
            result.HasLocationInDescription = !string.IsNullOrEmpty(result.MetaDescription);

            return result;
        }

        public async Task<StructuredDataAnalysis> AnalyzeStructuredData(string content)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(content);

            var result = new StructuredDataAnalysis();
            var structuredData = doc.DocumentNode.SelectNodes("//script[@type='application/ld+json']");

            result.HasStructuredData = structuredData != null;
            result.IsValid = result.HasStructuredData;

            return result;
        }

        public async Task<UrlAnalysis> AnalyzeUrlStructure(string content)
        {
            // Note: Since we don't have access to the URL in this method,
            // we'll need to modify this when we update the interface
            var result = new UrlAnalysis
            {
                HasLocationInUrl = true, // Placeholder
                IsUrlOptimized = true,   // Placeholder
                CurrentUrlStructure = "example.com" // Placeholder
            };

            return result;
        }

        private int CalculateOverallScore(LocalSeoAnalysisResult result)
        {
            var score = 0;

            if (result.NapDetails.IsNapConsistent) score += 25;
            if (result.SchemaMarkup.HasLocalBusinessSchema) score += 20;
            if (result.KeywordsAnalysis.HasSufficientLocationKeywords) score += 15;
            if (result.HasGoogleMapsEmbedded) score += 10;
            if (result.MetaInformation.HasLocationInTitle) score += 15;
            if (result.UrlStructure.IsUrlOptimized) score += 15;

            return Math.Min(score, 100);
        }

        private List<string> GenerateRecommendations(LocalSeoAnalysisResult result)
        {
            var recommendations = new List<string>();

            if (!result.NapDetails.IsNapConsistent)
                recommendations.Add("Ensure NAP information is consistent across all pages");

            if (!result.SchemaMarkup.HasLocalBusinessSchema)
                recommendations.Add("Implement LocalBusiness schema markup");

            if (!result.HasGoogleMapsEmbedded)
                recommendations.Add("Add an embedded Google Maps to your contact page");

            if (!result.MetaInformation.HasLocationInTitle)
                recommendations.Add("Include your location in the page title");

            if (!result.MetaInformation.HasLocationInDescription)
                recommendations.Add("Add location information to your meta description");

            return recommendations;
        }

        private string ExtractBusinessName(HtmlDocument doc)
        {
            // Implementation details - you can enhance this based on your needs
            return doc.DocumentNode.SelectSingleNode("//h1")?.InnerText ?? "";
        }

        private string ExtractAddress(HtmlDocument doc)
        {
            // Implementation details - you can enhance this based on your needs
            return doc.DocumentNode.SelectSingleNode("//address")?.InnerText ?? "";
        }

        private string ExtractPhone(HtmlDocument doc)
        {
            // Implementation details - you can enhance this based on your needs
            var phonePattern = @"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b";
            var text = doc.DocumentNode.InnerText;
            var match = Regex.Match(text, phonePattern);
            return match.Success ? match.Value : "";
        }
    }
}
