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
        private readonly SearchHistoryService _searchHistoryService;
        private readonly ILogger<BusinessSearchController> _logger;

        public BusinessSearchController(
            BusinessDataService businessDataService,
            CrmService crmService,
            SearchHistoryService searchHistoryService,
            ILogger<BusinessSearchController> logger)
        {
            _businessDataService = businessDataService;
            _crmService = crmService;
            _searchHistoryService = searchHistoryService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int? displayCount)
        {
            var count = displayCount ?? 5; // Default to 5 if not specified
            var recentSearches = await _searchHistoryService.GetRecentSearchesAsync(count);
            var viewModel = new RecentSearchesViewModel
            {
                RecentSearches = recentSearches,
                DisplayCount = count
            };
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Search(string industry, string zipcode, int? limit)
        {
            try
            {
                _logger.LogInformation($"Searching for {industry} businesses in {zipcode} with limit: {limit ?? 5}");
                var businesses = await _businessDataService.SearchBusinessesAsync(industry, zipcode, limit);
                _logger.LogInformation($"Found {businesses.Count} businesses");

                // Save search to history
                await _searchHistoryService.SaveSearchAsync(industry, zipcode, limit ?? 5, businesses);

                return View("Results", businesses);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during business search: {ex.Message}");
                TempData["Error"] = "An error occurred while searching for businesses. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> History(
            int page = 1,
            string? sortBy = null,
            bool ascending = true,
            string? industryFilter = null,
            string? zipCodeFilter = null)
        {
            try
            {
                const int PageSize = 10;
                var (searches, totalCount) = await _searchHistoryService.GetSearchHistoryAsync(
                    page, PageSize, sortBy, ascending, industryFilter, zipCodeFilter);

                var viewModel = new SearchHistoryViewModel
                {
                    Searches = searches,
                    Pagination = new PaginationInfo
                    {
                        CurrentPage = page,
                        PageSize = PageSize,
                        TotalItems = totalCount,
                        TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize)
                    },
                    Filter = new SearchHistoryFilter
                    {
                        Industry = industryFilter,
                        ZipCode = zipCodeFilter
                    },
                    CurrentSort = sortBy,
                    IsAscending = ascending
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving search history: {ex.Message}");
                TempData["Error"] = "An error occurred while retrieving search history.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> ViewHistoricalResults(int id)
        {
            try
            {
                var savedSearch = await _searchHistoryService.GetSearchByIdAsync(id);
                if (savedSearch == null)
                {
                    TempData["Error"] = "Search not found.";
                    return RedirectToAction(nameof(History));
                }

                // Convert SavedBusinessResults back to Business objects
                var businesses = savedSearch.Results.Select(r => new Business
                {
                    BusinessId = r.BusinessId,
                    Name = r.Name,
                    PhoneNumber = r.PhoneNumber,
                    Latitude = r.Latitude,
                    Longitude = r.Longitude,
                    FullAddress = r.FullAddress,
                    ReviewCount = r.ReviewCount,
                    Rating = r.Rating,
                    OpeningStatus = r.OpeningStatus,
                    Website = r.Website,
                    Verified = r.Verified,
                    PlaceLink = r.PlaceLink,
                    ReviewsLink = r.ReviewsLink,
                    OwnerName = r.OwnerName,
                    BusinessStatus = r.BusinessStatus,
                    Type = r.Type,
                    PhotoCount = r.PhotoCount,
                    PriceLevel = r.PriceLevel,
                    StreetAddress = r.StreetAddress,
                    City = r.City,
                    Zipcode = r.Zipcode,
                    State = r.State,
                    Country = r.Country,
                    Email = r.Email,
                    PhotoUrl = r.PhotoUrl,
                    Facebook = r.Facebook,
                    Instagram = r.Instagram,
                    YelpUrl = r.YelpUrl
                }).ToList();

                ViewBag.SearchDate = savedSearch.SearchDate;
                return View("Results", businesses);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving historical results: {ex.Message}");
                TempData["Error"] = "An error occurred while retrieving the search results.";
                return RedirectToAction(nameof(History));
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSearch(int id)
        {
            try
            {
                await _searchHistoryService.DeleteSearchAsync(id);
                TempData["Success"] = "Search history deleted successfully.";
                return RedirectToAction(nameof(History));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting search history: {ex.Message}");
                TempData["Error"] = "An error occurred while deleting the search history.";
                return RedirectToAction(nameof(History));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCrm([FromBody] AddToCrmRequest request)
        {
            try
            {
                _logger.LogInformation($"Adding business {request.Name} to CRM list {request.ListId}");

                var crmEntry = new CrmEntry
                {
                    BusinessName = request.Name,
                    Phone = request.PhoneNumber,
                    Email = request.Email,
                    Website = request.Website,
                    Industry = request.Type,
                    DateAdded = DateTime.UtcNow,
                    Disposition = request.Disposition ?? "New",
                    Notes = request.Notes,
                    GoogleRating = request.Rating,
                    ReviewCount = request.ReviewCount,
                    PhotoUrl = request.PhotoUrl,
                    BusinessStatus = request.BusinessStatus,
                    OpeningStatus = request.OpeningStatus,
                    FullAddress = request.FullAddress,
                    Facebook = request.Facebook,
                    Instagram = request.Instagram,
                    YelpUrl = request.YelpUrl
                };

                // Add to CRM with specified list
                await _crmService.AddEntry(crmEntry, request.ListId);
                _logger.LogInformation($"Successfully added {request.Name} to CRM list {request.ListId}");
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

        public class AddToCrmRequest
        {
            public string Name { get; set; }
            public string? PhoneNumber { get; set; }
            public string? Email { get; set; }
            public string? Website { get; set; }
            public string? Type { get; set; }
            public string? FullAddress { get; set; }
            public double? Rating { get; set; }
            public int? ReviewCount { get; set; }
            public string? PhotoUrl { get; set; }
            public string? BusinessStatus { get; set; }
            public string? OpeningStatus { get; set; }
            public string? Facebook { get; set; }
            public string? Instagram { get; set; }
            public string? YelpUrl { get; set; }
            public string? Disposition { get; set; }
            public string? Notes { get; set; }
            public int ListId { get; set; }
        }
    }
}