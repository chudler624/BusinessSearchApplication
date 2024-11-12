using BusinessSearch.Models.WebsiteAnalysis;
using HtmlAgilityPack;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BusinessSearch.Services.WebsiteOpportunitiesServices.Interfaces
{
    public interface IAccessibilityService
    {
        Task<AccessibilityAnalysisResult> AnalyzeAccessibilityAsync(string url);
        Task<List<AccessibilityIssue>> CheckImageAccessibilityAsync(HtmlDocument document);
        Task<List<AccessibilityIssue>> CheckHeadingStructureAsync(HtmlDocument document);
        Task<List<AccessibilityIssue>> CheckColorContrastAsync(HtmlDocument document);
        Task<List<AccessibilityIssue>> CheckFormAccessibilityAsync(HtmlDocument document);
        Task<List<AccessibilityIssue>> CheckKeyboardNavigationAsync(HtmlDocument document);
        double CalculateComplianceScore(List<AccessibilityIssue> issues);
    }
}
