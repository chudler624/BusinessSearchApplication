using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using BusinessSearch.Services;
using BusinessSearch.Models;
using System;
using System.Diagnostics;

namespace BusinessSearch.Controllers
{
    public class BusinessSearchController : Controller
    {
        private readonly BusinessDataService _businessDataService;
        private readonly CrmService _crmService;

        public BusinessSearchController(BusinessDataService businessDataService, CrmService crmService)
        {
            _businessDataService = businessDataService;
            _crmService = crmService;
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
                var crmEntry = new CrmEntry
                {
                    BusinessName = business.Name,
                    Phone = business.PhoneNumber,
                    Email = business.Email,
                    Website = business.Website,
                    Industry = business.Type,
                    DateAdded = DateTime.UtcNow,
                    Disposition = "New",
                    Notes = $"Added from search results on {DateTime.UtcNow}"
                };

                await _crmService.AddEntry(crmEntry);
                return Json(new { success = true, message = "Business successfully added to CRM" });
            }
            catch (Exception ex)
            {
                // Log the error
                Debug.WriteLine($"Error adding to CRM: {ex.Message}");
                return Json(new { success = false, message = "Failed to add business to CRM" });
            }
        }
    }
}