using BusinessSearch.Models;
using BusinessSearch.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using BusinessSearch.Models.ViewModels;
using BusinessSearch.Models.Organization;
using Microsoft.AspNetCore.Authorization;

namespace BusinessSearch.Controllers
{
    [Authorize]
    public class BusinessSearchController : Controller
    {
        private readonly BusinessDataService _businessDataService;
        private readonly CrmService _crmService;
        private readonly SearchHistoryService _searchHistoryService;
        private readonly ISearchUsageService _searchUsageService;
        private readonly IOrganizationFilterService _orgFilter;
        private readonly ILogger<BusinessSearchController> _logger;

        public BusinessSearchController(
            BusinessDataService businessDataService,
            CrmService crmService,
            SearchHistoryService searchHistoryService,
            ISearchUsageService searchUsageService,
            IOrganizationFilterService orgFilter,
            ILogger<BusinessSearchController> logger)
        {
            _businessDataService = businessDataService;
            _crmService = crmService;
            _searchHistoryService = searchHistoryService;
            _searchUsageService = searchUsageService;
            _orgFilter = orgFilter;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int? displayCount)
        {
            try
            {
                var organization = await _orgFilter.GetCurrentOrganization();
                if (organization == null)
                {
                    return RedirectToAction("NoOrganization", "Account");
                }

                var count = displayCount ?? 5; // Default to 5 if not specified
                var recentSearches = await _searchHistoryService.GetRecentSearchesAsync(count);
                var status = await _searchUsageService.GetSearchUsageStatusAsync(organization.Id);

                var viewModel = new RecentSearchesViewModel
                {
                    RecentSearches = recentSearches,
                    DisplayCount = count,
                    SearchUsage = status,
                    OrganizationPlan = organization.Plan
                };
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading search index");
                TempData["Error"] = "An error occurred while loading the page.";
                return RedirectToAction("NoOrganization", "Account");
            }
        }

        [HttpGet]
        public IActionResult Search()
        {
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Search(string industry, string zipcode, int? limit)
        {
            try
            {
                var organization = await _orgFilter.GetCurrentOrganization();
                if (organization == null)
                {
                    return RedirectToAction("NoOrganization", "Account");
                }

                var maxResults = limit ?? 5;
                if (!await _searchUsageService.CanPerformSearchAsync(organization.Id, maxResults))
                {
                    TempData["Error"] = "Daily search results limit reached. Please try again tomorrow or upgrade your plan.";
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogInformation($"Searching for {industry} businesses in {zipcode} with limit: {maxResults}");
                var businesses = await _businessDataService.SearchBusinessesAsync(industry, zipcode, limit);
                _logger.LogInformation($"Found {businesses.Count} businesses");

                // Increment by actual results returned, not the requested limit
                await _searchUsageService.IncrementSearchResultsCountAsync(organization.Id, businesses.Count);
                await _searchHistoryService.SaveSearchAsync(industry, zipcode, maxResults, businesses);

                // Pass the current usage status to the view
                var searchStatus = await _searchUsageService.GetSearchUsageStatusAsync(organization.Id);
                ViewBag.SearchUsage = searchStatus;
                ViewBag.OrganizationPlan = organization.Plan;

                return View("Results", businesses);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during business search: {ex.Message}");
                TempData["Error"] = "An error occurred while searching for businesses. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> History(
            int page = 1,
            string? sortBy = null,
            bool ascending = true,
            string? industryFilter = null,
            string? zipCodeFilter = null)
        {
            try
            {
                var organization = await _orgFilter.GetCurrentOrganization();
                if (organization == null)
                {
                    return RedirectToAction("NoOrganization", "Account");
                }

                const int PageSize = 10;
                var (searches, totalCount) = await _searchHistoryService.GetSearchHistoryAsync(
                    page, PageSize, sortBy, ascending, industryFilter, zipCodeFilter);

                var searchStatus = await _searchUsageService.GetSearchUsageStatusAsync(organization.Id);

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
                    IsAscending = ascending,
                    SearchUsage = searchStatus,
                    OrganizationPlan = organization.Plan
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
                var organization = await _orgFilter.GetCurrentOrganization();
                if (organization == null)
                {
                    return RedirectToAction("NoOrganization", "Account");
                }

                var savedSearch = await _searchHistoryService.GetSearchByIdAsync(id);
                if (savedSearch == null)
                {
                    TempData["Error"] = "Search not found.";
                    return RedirectToAction(nameof(History));
                }

                // Get current search usage status
                var searchStatus = await _searchUsageService.GetSearchUsageStatusAsync(organization.Id);

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

                ViewBag.SearchUsage = searchStatus;
                ViewBag.OrganizationPlan = organization.Plan;
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

        [HttpGet]
        public async Task<IActionResult> ViewMultipleHistoricalResults(string ids)
        {
            try
            {
                var organization = await _orgFilter.GetCurrentOrganization();
                if (organization == null)
                {
                    return RedirectToAction("NoOrganization", "Account");
                }

                if (string.IsNullOrEmpty(ids))
                {
                    TempData["Error"] = "No search IDs provided.";
                    return RedirectToAction(nameof(History));
                }

                var searchIdList = ids.Split(',')
                    .Select(id => int.TryParse(id, out int parsedId) ? parsedId : -1)
                    .Where(id => id > 0)
                    .ToList();

                if (!searchIdList.Any())
                {
                    TempData["Error"] = "Invalid search IDs provided.";
                    return RedirectToAction(nameof(History));
                }

                // Get the combined business results from all specified searches
                var combinedBusinesses = new List<Business>();
                var processedResults = new HashSet<string>(); // Track unique business IDs to avoid duplicates
                var searchDates = new List<DateTime>();

                foreach (var searchId in searchIdList)
                {
                    var savedSearch = await _searchHistoryService.GetSearchByIdAsync(searchId);
                    if (savedSearch != null)
                    {
                        searchDates.Add(savedSearch.SearchDate);

                        foreach (var result in savedSearch.Results)
                        {
                            // Only add unique businesses (based on business ID)
                            if (!processedResults.Contains(result.BusinessId))
                            {
                                processedResults.Add(result.BusinessId);

                                // Convert SavedBusinessResult to Business object
                                combinedBusinesses.Add(new Business
                                {
                                    BusinessId = result.BusinessId,
                                    Name = result.Name,
                                    PhoneNumber = result.PhoneNumber,
                                    Latitude = result.Latitude,
                                    Longitude = result.Longitude,
                                    FullAddress = result.FullAddress,
                                    ReviewCount = result.ReviewCount,
                                    Rating = result.Rating,
                                    OpeningStatus = result.OpeningStatus,
                                    Website = result.Website,
                                    Verified = result.Verified,
                                    PlaceLink = result.PlaceLink,
                                    ReviewsLink = result.ReviewsLink,
                                    OwnerName = result.OwnerName,
                                    BusinessStatus = result.BusinessStatus,
                                    Type = result.Type,
                                    PhotoCount = result.PhotoCount,
                                    PriceLevel = result.PriceLevel,
                                    StreetAddress = result.StreetAddress,
                                    City = result.City,
                                    Zipcode = result.Zipcode,
                                    State = result.State,
                                    Country = result.Country,
                                    Email = result.Email,
                                    PhotoUrl = result.PhotoUrl,
                                    Facebook = result.Facebook,
                                    Instagram = result.Instagram,
                                    YelpUrl = result.YelpUrl
                                });
                            }
                        }
                    }
                }

                // Get current search usage status
                var searchStatus = await _searchUsageService.GetSearchUsageStatusAsync(organization.Id);

                ViewBag.SearchUsage = searchStatus;
                ViewBag.OrganizationPlan = organization.Plan;
                ViewBag.SearchDate = searchDates.Count > 0
                    ? searchDates.Max() // Show the most recent date
                    : (DateTime?)null;
                ViewBag.CombinedResults = true;
                ViewBag.NumberOfSearches = searchIdList.Count;

                return View("Results", combinedBusinesses);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving multiple historical results: {ex.Message}");
                TempData["Error"] = "An error occurred while retrieving the combined search results.";
                return RedirectToAction(nameof(History));
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSearch(int id)
        {
            try
            {
                var organization = await _orgFilter.GetCurrentOrganization();
                if (organization == null)
                {
                    return RedirectToAction("NoOrganization", "Account");
                }

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
        public async Task<IActionResult> DeleteMultipleSearches([FromBody] DeleteMultipleSearchesRequest request)
        {
            try
            {
                var organization = await _orgFilter.GetCurrentOrganization();
                if (organization == null)
                {
                    return Json(new { success = false, message = "Organization not found" });
                }

                if (request.SearchIds == null || !request.SearchIds.Any())
                {
                    return Json(new { success = false, message = "No search IDs provided" });
                }

                _logger.LogInformation($"Deleting {request.SearchIds.Count} search history entries");

                int deletedCount = 0;
                foreach (var id in request.SearchIds)
                {
                    try
                    {
                        await _searchHistoryService.DeleteSearchAsync(id);
                        deletedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error deleting search history entry {id}: {ex.Message}");
                    }
                }

                return Json(new { success = true, deletedCount = deletedCount });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting multiple search history entries: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while deleting search history entries" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCrm([FromBody] AddToCrmRequest request)
        {
            try
            {
                var organization = await _orgFilter.GetCurrentOrganization();
                if (organization == null)
                {
                    return Json(new { success = false, message = "Organization not found" });
                }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMultipleToCrm([FromBody] AddMultipleToCrmRequest request)
        {
            try
            {
                var organization = await _orgFilter.GetCurrentOrganization();
                if (organization == null)
                {
                    return Json(new { success = false, message = "Organization not found" });
                }

                _logger.LogInformation($"Adding {request.Businesses.Count} businesses to CRM list {request.SelectedListId}");

                int successCount = 0;
                var errors = new List<string>();

                foreach (var business in request.Businesses)
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
                            Disposition = request.CommonData.Disposition ?? "New",
                            Notes = request.CommonData.Notes,
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

                        // Add to CRM with specified list
                        await _crmService.AddEntry(crmEntry, request.SelectedListId);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error adding business {business.Name} to CRM: {ex.Message}");
                        errors.Add($"Failed to add {business.Name}: {ex.Message}");
                    }
                }

                if (successCount == request.Businesses.Count)
                {
                    _logger.LogInformation($"Successfully added all {successCount} businesses to CRM list {request.SelectedListId}");
                    return Json(new { success = true, message = $"Successfully added all {successCount} businesses to CRM" });
                }
                else if (successCount > 0)
                {
                    _logger.LogWarning($"Added {successCount} out of {request.Businesses.Count} businesses to CRM list {request.SelectedListId}");
                    return Json(new
                    {
                        success = true,
                        message = $"Added {successCount} out of {request.Businesses.Count} businesses to CRM",
                        warnings = errors
                    });
                }
                else
                {
                    _logger.LogError($"Failed to add any businesses to CRM list {request.SelectedListId}");
                    return Json(new { success = false, message = "Failed to add any businesses to CRM", errors = errors });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in batch adding businesses to CRM: {ex.Message}");
                return Json(new { success = false, message = "Failed to add businesses to CRM" });
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

        public class AddMultipleToCrmRequest
        {
            public List<BusinessData> Businesses { get; set; } = new List<BusinessData>();
            public CommonDataModel CommonData { get; set; } = new CommonDataModel();
            public int SelectedListId { get; set; }

            public class BusinessData
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
            }

            public class CommonDataModel
            {
                public string? Disposition { get; set; }
                public string? Notes { get; set; }
                public DateTime? DateAdded { get; set; }
            }
        }

        public class DeleteMultipleSearchesRequest
        {
            public List<int> SearchIds { get; set; } = new List<int>();
        }
    }
}