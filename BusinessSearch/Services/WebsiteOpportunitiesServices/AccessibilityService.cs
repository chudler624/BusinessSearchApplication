using BusinessSearch.Models.WebsiteAnalysis;
using BusinessSearch.Services.WebsiteOpportunitiesServices.Interfaces;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace BusinessSearch.Services.WebsiteOpportunitiesServices
{
    public class AccessibilityService : IAccessibilityService
    {
        private readonly IHttpClientFactory _clientFactory;

        public AccessibilityService(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        }

        public async Task<AccessibilityAnalysisResult> AnalyzeAccessibilityAsync(string url)
        {
            var result = new AccessibilityAnalysisResult
            {
                PageUrl = url
            };

            var web = new HtmlWeb();
            var document = await web.LoadFromWebAsync(url);

            var allIssues = new List<AccessibilityIssue>();

            // Run all checks
            allIssues.AddRange(await CheckImageAccessibilityAsync(document));
            allIssues.AddRange(await CheckHeadingStructureAsync(document));
            allIssues.AddRange(await CheckColorContrastAsync(document));
            allIssues.AddRange(await CheckFormAccessibilityAsync(document));
            allIssues.AddRange(await CheckKeyboardNavigationAsync(document));

            result.Issues = allIssues;
            result.ComplianceScore = CalculateComplianceScore(allIssues);

            // Calculate issues by level
            result.IssueCountByLevel = allIssues
                .GroupBy(i => i.Level)
                .ToDictionary(g => g.Key, g => g.Count());

            return result;
        }

        public async Task<List<AccessibilityIssue>> CheckImageAccessibilityAsync(HtmlDocument document)
        {
            var issues = new List<AccessibilityIssue>();
            var images = document.DocumentNode.SelectNodes("//img");

            if (images == null) return issues;

            foreach (var img in images)
            {
                var alt = img.GetAttributeValue("alt", null);

                if (string.IsNullOrWhiteSpace(alt))
                {
                    issues.Add(new AccessibilityIssue
                    {
                        Criterion = "1.1.1 Non-text Content",
                        Level = WcagLevel.A,
                        Description = "Image missing alt text",
                        Element = img.OuterHtml,
                        Recommendation = "Add descriptive alt text to the image",
                        Impact = "Screen readers cannot describe the image to visually impaired users"
                    });
                }
            }

            return issues;
        }

        public async Task<List<AccessibilityIssue>> CheckHeadingStructureAsync(HtmlDocument document)
        {
            var issues = new List<AccessibilityIssue>();
            var headings = document.DocumentNode.SelectNodes("//h1 | //h2 | //h3 | //h4 | //h5 | //h6");

            if (headings == null) return issues;

            // Check for multiple H1s
            var h1Count = headings.Count(h => h.Name == "h1");
            if (h1Count > 1)
            {
                issues.Add(new AccessibilityIssue
                {
                    Criterion = "2.4.6 Headings and Labels",
                    Level = WcagLevel.AA,
                    Description = "Multiple H1 headings found",
                    Element = "Multiple <h1> tags",
                    Recommendation = "Use only one H1 heading per page",
                    Impact = "Multiple H1s can confuse screen reader users about the main topic"
                });
            }

            return issues;
        }

        public async Task<List<AccessibilityIssue>> CheckColorContrastAsync(HtmlDocument document)
        {
            var issues = new List<AccessibilityIssue>();
            var textElements = document.DocumentNode.SelectNodes("//*[text()]");

            if (textElements == null) return issues;

            // Basic check for now - this would need a more sophisticated implementation
            // to actually compute contrast ratios from CSS
            return issues;
        }

        public async Task<List<AccessibilityIssue>> CheckFormAccessibilityAsync(HtmlDocument document)
        {
            var issues = new List<AccessibilityIssue>();
            var formControls = document.DocumentNode.SelectNodes("//input | //select | //textarea");

            if (formControls == null) return issues;

            foreach (var control in formControls)
            {
                var id = control.GetAttributeValue("id", "");
                var ariaLabel = control.GetAttributeValue("aria-label", "");
                var ariaLabelledBy = control.GetAttributeValue("aria-labelledby", "");

                if (string.IsNullOrEmpty(id) && string.IsNullOrEmpty(ariaLabel) &&
                    string.IsNullOrEmpty(ariaLabelledBy))
                {
                    issues.Add(new AccessibilityIssue
                    {
                        Criterion = "3.3.2 Labels or Instructions",
                        Level = WcagLevel.A,
                        Description = "Form control missing label",
                        Element = control.OuterHtml,
                        Recommendation = "Add label or aria-label to form control",
                        Impact = "Screen reader users cannot identify the purpose of the form control"
                    });
                }
            }

            return issues;
        }

        public async Task<List<AccessibilityIssue>> CheckKeyboardNavigationAsync(HtmlDocument document)
        {
            var issues = new List<AccessibilityIssue>();
            var interactiveElements = document.DocumentNode.SelectNodes(
                "//a | //button | //input | //select | //textarea | //*[@onclick or @onkeyup or @onkeydown or @onkeypress]");

            if (interactiveElements == null) return issues;

            foreach (var element in interactiveElements)
            {
                var tabIndex = element.GetAttributeValue("tabindex", "0");
                if (tabIndex == "-1")
                {
                    issues.Add(new AccessibilityIssue
                    {
                        Criterion = "2.1.1 Keyboard",
                        Level = WcagLevel.A,
                        Description = "Interactive element not keyboard accessible",
                        Element = element.OuterHtml,
                        Recommendation = "Remove tabindex=-1 or ensure element can be accessed via keyboard",
                        Impact = "Keyboard users cannot access this element"
                    });
                }
            }

            return issues;
        }

        public double CalculateComplianceScore(List<AccessibilityIssue> issues)
        {
            if (!issues.Any()) return 100;

            var weights = new Dictionary<WcagLevel, double>
            {
                { WcagLevel.A, 1.0 },
                { WcagLevel.AA, 0.8 },
                { WcagLevel.AAA, 0.6 }
            };

            var weightedIssueCount = issues.Sum(i => weights[i.Level]);
            var maxScore = 100;
            var deductionPerIssue = 5;

            return Math.Max(0, maxScore - (weightedIssueCount * deductionPerIssue));
        }
    }
}