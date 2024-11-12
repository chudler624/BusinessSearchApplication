using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using BusinessSearch.Services.WebsiteOpportunitiesServices;
using BusinessSearch.Services.WebsiteOpportunitiesServices.Interfaces;

namespace BusinessSearch.Controllers
{
    public class WebsiteOpportunitiesController : Controller
    {
        private readonly IWebsiteOpportunitiesService _websiteOpportunitiesService;
        private readonly IAccessibilityService _accessibilityService;

        public WebsiteOpportunitiesController(
            IWebsiteOpportunitiesService websiteOpportunitiesService,
            IAccessibilityService accessibilityService)
        {
            _websiteOpportunitiesService = websiteOpportunitiesService;
            _accessibilityService = accessibilityService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AnalyzeWebsite(string url)
        {
            var websiteAnalysis = await _websiteOpportunitiesService.AnalyzeWebsite(url);
            var accessibilityAnalysis = await _accessibilityService.AnalyzeAccessibilityAsync(url);

            var result = new
            {
                responsivenessResult = websiteAnalysis.ResponsivenessResult,
                gdprComplianceResult = websiteAnalysis.GdprComplianceResult,
                pageSpeedResult = websiteAnalysis.PageSpeedResult,
                accessibilityResult = accessibilityAnalysis
            };

            return Json(result);
        }
    }
}