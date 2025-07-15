using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using BusinessSearch.Models.WebsiteAnalysis;
using BusinessSearch.Services.WebsiteOpportunitiesServices.Interfaces;

namespace BusinessSearch.Services.WebsiteOpportunitiesServices
{
    public class PageSpeedService : IPageSpeedService
    {
        public async Task<PageSpeedResult> AnalyzePageSpeed(string content, double ttfb, double totalLoadTime)
        {
            var result = new PageSpeedResult
            {
                WebVitals = new WebVitals  // Changed from CoreWebVitals to WebVitals
                {
                    TimeToFirstByte = ttfb,
                    LargestContentfulPaint = totalLoadTime / 1000.0,
                    CumulativeLayoutShift = 0.1
                },
                ResourceMetrics = AnalyzeResources(content)
            };

            var scores = new List<(double score, double weight)>
            {
                (CalculateTTFBScore(ttfb), 0.2),
                (CalculateResourceScore(result.ResourceMetrics), 0.3),
                (CalculateLCPScore(result.WebVitals.LargestContentfulPaint), 0.3),
                (CalculateCLSScore(result.WebVitals.CumulativeLayoutShift), 0.2)
            };

            result.Score = scores.Sum(s => s.score * s.weight);
            result.IsFast = result.Score >= 80;
            result.Details = GeneratePerformanceInsights(result);

            return result;
        }

        private ResourceMetrics AnalyzeResources(string content)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(content);

            var metrics = new ResourceMetrics
            {
                TotalPageSize = content.Length,
                ResourceCounts = new Dictionary<string, int>
                {
                    ["Scripts"] = doc.DocumentNode.SelectNodes("//script")?.Count ?? 0,
                    ["Stylesheets"] = doc.DocumentNode.SelectNodes("//link[@rel='stylesheet']")?.Count ?? 0,
                    ["Images"] = doc.DocumentNode.SelectNodes("//img")?.Count ?? 0,
                    ["Fonts"] = doc.DocumentNode.SelectNodes("//link[@rel='preload'][@as='font']")?.Count ?? 0
                }
            };

            metrics.TotalRequests = metrics.ResourceCounts.Values.Sum();
            return metrics;
        }

        private List<string> GeneratePerformanceInsights(PageSpeedResult result)
        {
            var insights = new List<string>();

            if (result.WebVitals.TimeToFirstByte > 600)
            {
                insights.Add($"Slow server response time ({result.WebVitals.TimeToFirstByte:F0}ms). Consider optimizing server performance.");
            }

            if (result.ResourceMetrics.ResourceCounts["Scripts"] > 15)
            {
                insights.Add($"High number of script files ({result.ResourceMetrics.ResourceCounts["Scripts"]}). Consider bundling JavaScript files.");
            }

            if (result.ResourceMetrics.ResourceCounts["Stylesheets"] > 5)
            {
                insights.Add($"Multiple stylesheet files ({result.ResourceMetrics.ResourceCounts["Stylesheets"]}). Consider consolidating CSS files.");
            }

            var pageSizeMB = result.ResourceMetrics.TotalPageSize / (1024.0 * 1024.0);
            if (pageSizeMB > 2)
            {
                insights.Add($"Large page size ({pageSizeMB:F2}MB). Consider optimizing images and minifying resources.");
            }

            insights.Add($"Overall Performance Score: {result.Score:F0}/100 ({(result.Score >= 90 ? "Excellent" : result.Score >= 80 ? "Good" : result.Score >= 70 ? "Fair" : result.Score >= 60 ? "Poor" : "Critical")})");

            return insights;
        }

        private double CalculateTTFBScore(double ttfb) =>
            ttfb <= 200 ? 100 :
            ttfb <= 400 ? 90 :
            ttfb <= 600 ? 80 :
            ttfb <= 800 ? 70 :
            ttfb <= 1000 ? 60 : 50;

        private double CalculateResourceScore(ResourceMetrics metrics)
        {
            var totalScore = 100;

            if (metrics.ResourceCounts["Scripts"] > 15) totalScore -= 10;
            if (metrics.ResourceCounts["Stylesheets"] > 5) totalScore -= 10;
            if (metrics.ResourceCounts["Images"] > 20) totalScore -= 10;

            var pageSizeMB = metrics.TotalPageSize / (1024.0 * 1024.0);
            if (pageSizeMB > 3) totalScore -= 20;
            else if (pageSizeMB > 2) totalScore -= 10;

            return Math.Max(totalScore, 0);
        }

        private double CalculateLCPScore(double lcp) =>
            lcp <= 2.5 ? 100 :
            lcp <= 4 ? 75 :
            lcp <= 6 ? 50 : 25;

        private double CalculateCLSScore(double cls) =>
            cls <= 0.1 ? 100 :
            cls <= 0.25 ? 75 :
            cls <= 0.4 ? 50 : 25;
    }
}