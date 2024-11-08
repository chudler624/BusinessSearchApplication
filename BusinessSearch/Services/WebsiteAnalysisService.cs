using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace BusinessSearch.Services
{
    public class WebsiteAnalysisService : IWebsiteAnalysisService
    {
        private readonly IHttpClientFactory _clientFactory;

        public WebsiteAnalysisService(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task<WebsiteAnalysisResult> AnalyzeWebsite(string url)
        {
            var client = _clientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to fetch the website. Status code: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();

            var responsivenessResult = AnalyzeResponsiveness(content);
            var gdprResult = AnalyzeGdprCompliance(content);

            return new WebsiteAnalysisResult
            {
                ResponsivenessResult = responsivenessResult,
                GdprComplianceResult = gdprResult
            };
        }

        private ResponsivenessResult AnalyzeResponsiveness(string content)
        {
            var result = new ResponsivenessResult { Details = new List<string>(), Score = 0 };

            // Check for viewport meta tag (essential for mobile responsiveness)
            if (Regex.IsMatch(content, @"<meta[^>]*name=[""']viewport[""'][^>]*content=[""'][^""']*width\s*=\s*device-width[^""']*[""'][^>]*>"))
            {
                result.Score += 30;
                result.Details.Add("Proper viewport meta tag found (+30 points)");
            }
            else if (Regex.IsMatch(content, @"<meta[^>]*name=[""']viewport[""'][^>]*>"))
            {
                result.Score += 15;
                result.Details.Add("Viewport meta tag found, but may not be properly configured (+15 points)");
            }
            else
            {
                result.Details.Add("No viewport meta tag found");
            }

            // Add other responsiveness checks here (media queries, responsive frameworks, etc.)
            // ...

            // Set final responsiveness status
            result.IsResponsive = result.Score >= 50;
            result.Details.Add($"Total score: {result.Score}/100");
            result.Details.Add(result.IsResponsive ?
                "The website appears to be mobile responsive." :
                "The website may not be fully mobile responsive. Consider implementing more responsive design techniques.");

            return result;
        }

        private GdprComplianceResult AnalyzeGdprCompliance(string content)
        {
            var result = new GdprComplianceResult
            {
                HasCookieConsent = false,
                HasPrivacyPolicy = false,
                OtherComplianceIndicators = new List<string>()
            };

            // Check for cookie consent
            if (Regex.IsMatch(content, @"cookie(?:\s+consent|banner)", RegexOptions.IgnoreCase))
            {
                result.HasCookieConsent = true;
                result.OtherComplianceIndicators.Add("Cookie consent mechanism detected");
            }

            // Check for privacy policy
            if (Regex.IsMatch(content, @"privacy\s+policy", RegexOptions.IgnoreCase))
            {
                result.HasPrivacyPolicy = true;
                result.OtherComplianceIndicators.Add("Privacy policy detected");
            }

            // Check for other GDPR-related terms
            var gdprTerms = new[] { "data protection", "right to be forgotten", "data subject rights", "GDPR" };
            foreach (var term in gdprTerms)
            {
                if (Regex.IsMatch(content, term, RegexOptions.IgnoreCase))
                {
                    result.OtherComplianceIndicators.Add($"{term} mentioned");
                }
            }

            return result;
        }
    }
}