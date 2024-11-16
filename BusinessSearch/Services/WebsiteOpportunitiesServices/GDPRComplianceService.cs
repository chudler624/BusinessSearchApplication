using System.Text.RegularExpressions;
using BusinessSearch.Services.WebsiteOpportunitiesServices.Interfaces;
using BusinessSearch.Models.WebsiteAnalysis;

namespace BusinessSearch.Services.WebsiteOpportunitiesServices
{
    public class GdprComplianceService : IGdprComplianceService
    {
        public GdprComplianceResult AnalyzeGdprCompliance(string content)
        {
            var result = new GdprComplianceResult
            {
                HasCookieConsent = false,
                HasPrivacyPolicy = false,
                ComplianceIndicators = new List<string>()
            };

            // Check for cookie consent
            if (Regex.IsMatch(content, @"cookie(?:\s+consent|banner)", RegexOptions.IgnoreCase))
            {
                result.HasCookieConsent = true;
                result.ComplianceIndicators.Add("Cookie consent mechanism detected");
            }

            // Check for privacy policy
            if (Regex.IsMatch(content, @"privacy\s+policy", RegexOptions.IgnoreCase))
            {
                result.HasPrivacyPolicy = true;
                result.ComplianceIndicators.Add("Privacy policy detected");
            }

            // Check for other GDPR-related terms
            var gdprTerms = new[] { "data protection", "right to be forgotten", "data subject rights", "GDPR" };
            foreach (var term in gdprTerms)
            {
                if (Regex.IsMatch(content, term, RegexOptions.IgnoreCase))
                {
                    result.ComplianceIndicators.Add($"{term} mentioned");
                }
            }

            return result;
        }
    }
}