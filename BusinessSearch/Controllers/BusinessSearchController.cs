using BusinessSearch.Models;
using BusinessSearch.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using BusinessSearch.Models.ViewModels;

namespace BusinessSearch.Controllers
{
    public class BusinessSearchController : Controller
    {
        private readonly BusinessDataService _businessDataService;
        private readonly CrmService _crmService;
        private readonly ILogger<BusinessSearchController> _logger;

        public BusinessSearchController(BusinessDataService businessDataService, CrmService crmService, ILogger<BusinessSearchController> logger)
        {
            _businessDataService = businessDataService;
            _crmService = crmService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Search(string industry, string zipcode)
        {
            var businesses = await _businessDataService.SearchBusinessesAsync(industry, zipcode);
            return View("Results", businesses);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCrm([FromForm] Business business)
        {
            try
            {
                _logger.LogInformation($"Adding business to CRM: {business.Name}");

                var crmEntry = new CrmEntry
                {
                    BusinessName = business.Name,
                    Phone = business.PhoneNumber,
                    Email = business.Email,
                    Website = business.Website,
                    Industry = business.Type,
                    DateAdded = DateTime.UtcNow,
                    Disposition = "New",
                    Notes = $"Added from search results on {DateTime.UtcNow}",
                    GoogleRating = business.Rating,
                    ReviewCount = business.ReviewCount,
                    PhotoUrl = business.PhotoUrl,
                    BusinessStatus = business.BusinessStatus,
                    OpeningStatus = business.OpeningStatus,
                    FullAddress = business.FullAddress,
                    Facebook = business.Facebook,
                    Instagram = business.Instagram,
                    YelpUrl = business.YelpUrl
                };

                await _crmService.AddEntry(crmEntry);
                _logger.LogInformation($"Successfully added {business.Name} to CRM");
                return Json(new { success = true, message = "Business successfully added to CRM" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding business to CRM: {ex.Message}");
                return Json(new { success = false, message = "Failed to add business to CRM" });
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}