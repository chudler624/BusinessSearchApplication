using BusinessSearch.Data;
using BusinessSearch.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BusinessSearch.Services
{
    public interface ISearchHistoryService
    {
        Task SaveSearchAsync(string industry, string zipCode, int limit, IEnumerable<Business> results);
        Task<SavedSearch?> GetSearchByIdAsync(int id);
        Task<(IEnumerable<SavedSearch> Searches, int TotalCount)> GetSearchHistoryAsync(
            int page = 1,
            int pageSize = 10,
            string? sortBy = null,
            bool ascending = true,
            string? industryFilter = null,
            string? zipCodeFilter = null);
        Task<IEnumerable<SavedSearch>> GetRecentSearchesAsync(int count);
        Task DeleteSearchAsync(int id);
    }

    public class SearchHistoryService : ISearchHistoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SearchHistoryService> _logger;
        private readonly IOrganizationFilterService _orgFilter;

        public SearchHistoryService(
            ApplicationDbContext context,
            ILogger<SearchHistoryService> logger,
            IOrganizationFilterService orgFilter)
        {
            _context = context;
            _logger = logger;
            _orgFilter = orgFilter;
        }

        public async Task SaveSearchAsync(string industry, string zipCode, int limit, IEnumerable<Business> results)
        {
            try
            {
                var orgId = await _orgFilter.GetCurrentOrganizationId();
                var savedSearch = new SavedSearch
                {
                    Industry = industry,
                    ZipCode = zipCode,
                    ResultLimit = limit,
                    SearchDate = DateTime.UtcNow,
                    TotalResults = results.Count(),
                    OrganizationId = orgId
                };

                _context.SavedSearches.Add(savedSearch);
                await _context.SaveChangesAsync();

                var savedResults = results.Select(business => new SavedBusinessResult
                {
                    SavedSearchId = savedSearch.Id,
                    BusinessId = business.BusinessId,
                    Name = business.Name,
                    PhoneNumber = business.PhoneNumber,
                    Latitude = business.Latitude,
                    Longitude = business.Longitude,
                    FullAddress = business.FullAddress,
                    ReviewCount = business.ReviewCount,
                    Rating = business.Rating,
                    OpeningStatus = business.OpeningStatus,
                    Website = business.Website,
                    Verified = business.Verified,
                    PlaceLink = business.PlaceLink,
                    ReviewsLink = business.ReviewsLink,
                    OwnerName = business.OwnerName,
                    BusinessStatus = business.BusinessStatus,
                    Type = business.Type,
                    SubtypesJson = business.Subtypes != null ? JsonSerializer.Serialize(business.Subtypes) : null,
                    PhotoCount = business.PhotoCount,
                    PriceLevel = business.PriceLevel,
                    StreetAddress = business.StreetAddress,
                    City = business.City,
                    Zipcode = business.Zipcode,
                    State = business.State,
                    Country = business.Country,
                    Email = business.Email,
                    PhotoUrl = business.PhotoUrl,
                    Facebook = business.Facebook,
                    Instagram = business.Instagram,
                    YelpUrl = business.YelpUrl
                }).ToList();

                _context.SavedBusinessResults.AddRange(savedResults);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving search history for {Industry} in {ZipCode}", industry, zipCode);
                throw;
            }
        }

        public async Task<SavedSearch?> GetSearchByIdAsync(int id)
        {
            var query = _context.SavedSearches
                .Include(s => s.Results)
                .Where(s => s.Id == id);

            query = _orgFilter.ApplyOrganizationFilter(query);
            return await query.FirstOrDefaultAsync();
        }

        public async Task<(IEnumerable<SavedSearch> Searches, int TotalCount)> GetSearchHistoryAsync(
            int page = 1,
            int pageSize = 10,
            string? sortBy = null,
            bool ascending = true,
            string? industryFilter = null,
            string? zipCodeFilter = null)
        {
            var query = _context.SavedSearches.AsQueryable();

            // Apply organization filter
            query = _orgFilter.ApplyOrganizationFilter(query);

            // Apply filters
            if (!string.IsNullOrWhiteSpace(industryFilter))
            {
                query = query.Where(s => s.Industry.Contains(industryFilter));
            }

            if (!string.IsNullOrWhiteSpace(zipCodeFilter))
            {
                query = query.Where(s => s.ZipCode.Contains(zipCodeFilter));
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply sorting
            query = sortBy?.ToLower() switch
            {
                "industry" => ascending
                    ? query.OrderBy(s => s.Industry)
                    : query.OrderByDescending(s => s.Industry),
                "zipcode" => ascending
                    ? query.OrderBy(s => s.ZipCode)
                    : query.OrderByDescending(s => s.ZipCode),
                "results" => ascending
                    ? query.OrderBy(s => s.TotalResults)
                    : query.OrderByDescending(s => s.TotalResults),
                _ => query.OrderByDescending(s => s.SearchDate) // Default sort
            };

            // Apply pagination
            var searches = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (searches, totalCount);
        }

        public async Task<IEnumerable<SavedSearch>> GetRecentSearchesAsync(int count)
        {
            var query = _context.SavedSearches.AsQueryable();
            query = _orgFilter.ApplyOrganizationFilter(query);

            return await query
                .OrderByDescending(s => s.SearchDate)
                .Take(count)
                .ToListAsync();
        }

        public async Task DeleteSearchAsync(int id)
        {
            var query = _context.SavedSearches.Where(s => s.Id == id);
            query = _orgFilter.ApplyOrganizationFilter(query);

            var search = await query.FirstOrDefaultAsync();
            if (search != null)
            {
                _context.SavedSearches.Remove(search);
                await _context.SaveChangesAsync();
            }
        }
    }
}