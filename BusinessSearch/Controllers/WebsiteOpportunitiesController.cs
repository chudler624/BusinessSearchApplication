using BusinessSearch.Services.WebsiteOpportunitiesServices;
using BusinessSearch.Services.WebsiteOpportunitiesServices.Interfaces;
using BusinessSearch.Models.WebsiteAnalysis;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BusinessSearch.Controllers
{
    public class WebsiteOpportunitiesController : Controller
    {
        private readonly IWebsiteOpportunitiesService _websiteOpportunitiesService;
        private readonly IAccessibilityService _accessibilityService;
        private readonly ILogger<WebsiteOpportunitiesController> _logger;

        public WebsiteOpportunitiesController(
            IWebsiteOpportunitiesService websiteOpportunitiesService,
            IAccessibilityService accessibilityService,
            ILogger<WebsiteOpportunitiesController> logger)
        {
            _websiteOpportunitiesService = websiteOpportunitiesService;
            _accessibilityService = accessibilityService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AnalyzeWebsite(string url)
        {
            try
            {
                if (string.IsNullOrEmpty(url))
                {
                    _logger.LogWarning("Attempt to analyze website with empty URL");
                    return BadRequest(new { message = "URL is required" });
                }

                _logger.LogInformation("Starting website analysis for URL: {Url}", url);

                WebsiteAnalysisModel websiteAnalysis = await _websiteOpportunitiesService.AnalyzeWebsite(url);
                var accessibilityAnalysis = await _accessibilityService.AnalyzeAccessibilityAsync(url);

                var result = new
                {
                    responsivenessResult = websiteAnalysis.ResponsivenessResult,
                    gdprComplianceResult = websiteAnalysis.GdprComplianceResult,
                    pageSpeedResult = websiteAnalysis.PageSpeedResult,
                    localSeoResult = websiteAnalysis.LocalSeoResult,
                    accessibilityResult = accessibilityAnalysis
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing website: {Url}", url);
                return StatusCode(500, new
                {
                    message = "An error occurred while analyzing the website",
                    error = ex.Message
                });
            }
        }
    }
}