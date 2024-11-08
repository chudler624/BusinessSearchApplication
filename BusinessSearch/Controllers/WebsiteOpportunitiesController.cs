using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using BusinessSearch.Services.WebsiteOpportunitiesServices;

namespace BusinessSearch.Controllers
{
    public class WebsiteOpportunitiesController : Controller
    {
        private readonly IWebsiteOpportunitiesService _websiteOpportunitiesService;

        public WebsiteOpportunitiesController(IWebsiteOpportunitiesService websiteOpportunitiesService)
        {
            _websiteOpportunitiesService = websiteOpportunitiesService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AnalyzeWebsite(string url)
        {
            var result = await _websiteOpportunitiesService.AnalyzeWebsite(url);
            return Json(result);
        }
    }
}