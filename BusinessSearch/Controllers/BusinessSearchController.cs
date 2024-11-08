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
        public async Task<IActionResult> AddToCrm([FromForm] Business business)
        {
            try
            {
                var crmEntry = new CrmEntry
                {
                    BusinessName = business.Name,
                    Phone = business.PhoneNumber,
                    Website = business.Website,
                    Industry = business.Type,
                    DateAdded = DateTime.UtcNow,
                    Disposition = "New",
                    Notes = $"Added from search results on {DateTime.UtcNow}"
                };

                await _crmService.AddEntry(crmEntry);
                TempData["Success"] = "Business successfully added to CRM";
                return RedirectToAction("Index", "Crm");
            }
            catch (Exception ex)
            {
                // Log the error
                Debug.WriteLine($"Error adding to CRM: {ex.Message}");
                TempData["Error"] = "Failed to add business to CRM";
                return RedirectToAction("Index", "Crm");
            }
        }
    }
}