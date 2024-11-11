using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using BusinessSearch.Models;

namespace BusinessSearch.Services
{
    public class BusinessDataService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _apiHost;
        private readonly IMemoryCache _cache;
        private readonly ILogger<BusinessDataService> _logger;

        public BusinessDataService(IConfiguration configuration, HttpClient httpClient, IMemoryCache memoryCache, ILogger<BusinessDataService> logger)
        {
            _httpClient = httpClient;
            _apiKey = configuration["RapidAPI:Key"];
            _apiHost = configuration["RapidAPI:Host"];
            _cache = memoryCache;
            _logger = logger;
        }

        public async Task<List<Business>> SearchBusinessesAsync(string query, string zipcode, int limit = 5)
        {
            var cacheKey = $"search_{query}_{zipcode}_{limit}";

            if (_cache.TryGetValue(cacheKey, out List<Business>? businesses))
            {
                _logger.LogInformation($"Cache hit for key: {cacheKey}");
                return businesses;
            }

            _logger.LogInformation($"Cache miss for key: {cacheKey}");

            // Default coordinates (can be adjusted or made dynamic)
            var latitude = "37.359428";
            var longitude = "-121.925337";

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(
                    $"https://local-business-data.p.rapidapi.com/search?" +
                    $"query={Uri.EscapeDataString(query)}%20in%20{zipcode}" +
                    $"&limit={limit}" +
                    $"&lat={latitude}" +
                    $"&lng={longitude}" +
                    $"&zoom=13" +
                    $"&language=en" +
                    $"&region=us" +
                    $"&extract_emails_and_contacts=true"
                )
            };

            _logger.LogInformation($"Making API request to URL: {request.RequestUri}");
            request.Headers.Add("x-rapidapi-key", _apiKey);
            request.Headers.Add("x-rapidapi-host", _apiHost);

            int retryCount = 0;
            const int maxRetries = 3;
            const int retryDelay = 2000;

            while (retryCount < maxRetries)
            {
                try
                {
                    var response = await _httpClient.SendAsync(request);
                    response.EnsureSuccessStatusCode();
                    var body = await response.Content.ReadAsStringAsync();

                    _logger.LogInformation("API Response received");
                    _logger.LogDebug($"Full API Response: {body}");

                    var jsonObject = JObject.Parse(body);
                    businesses = ParseBusinesses(jsonObject);

                    var cacheEntryOptions = new MemoryCacheEntryOptions()
                        .SetSlidingExpiration(TimeSpan.FromMinutes(30));

                    _cache.Set(cacheKey, businesses, cacheEntryOptions);
                    _logger.LogInformation($"Cached {businesses.Count} businesses for key: {cacheKey}");

                    return businesses;
                }
                catch (HttpRequestException ex) when (ex.Message.Contains("429"))
                {
                    retryCount++;
                    if (retryCount >= maxRetries)
                    {
                        _logger.LogError($"Rate limit exceeded after {maxRetries} attempts.");
                        throw;
                    }
                    _logger.LogWarning($"Rate limit exceeded. Retrying in {retryDelay}ms. Attempt {retryCount} of {maxRetries}");
                    await Task.Delay(retryDelay * retryCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error occurred while fetching business data: {ex.Message}");
                    _logger.LogError($"Stack trace: {ex.StackTrace}");
                    throw;
                }
            }

            throw new Exception("Failed to retrieve data after multiple attempts");
        }

        private List<Business> ParseBusinesses(JObject apiResult)
        {
            var businesses = new List<Business>();
            try
            {
                _logger.LogInformation("Starting to parse businesses from API result");
                _logger.LogDebug($"Full API result: {apiResult.ToString()}");

                var data = apiResult["data"] as JArray;
                if (data == null)
                {
                    _logger.LogWarning("No data array found in API result");
                    return businesses;
                }

                _logger.LogInformation($"Found {data.Count} businesses in data array");

                foreach (var item in data)
                {
                    try
                    {
                        _logger.LogDebug($"Processing business item: {item.ToString()}");

                        // Debug logging for contact information
                        var contactInfo = item["extracted_data"];
                        _logger.LogDebug($"Contact information: {contactInfo?.ToString() ?? "null"}");

                        var emails = contactInfo?["emails"] as JArray;
                        _logger.LogDebug($"Emails array: {emails?.ToString() ?? "null"}");

                        // Try to get email from different possible locations
                        string? email = null;

                        // Try main email field
                        if (item["email"] != null)
                        {
                            email = item["email"].ToString();
                            _logger.LogDebug($"Found email in main field: {email}");
                        }

                        // Try extracted_data emails
                        if (string.IsNullOrEmpty(email) && emails != null && emails.Count > 0)
                        {
                            email = emails[0].ToString();
                            _logger.LogDebug($"Found email in extracted_data: {email}");
                        }

                        // Try contact_information
                        if (string.IsNullOrEmpty(email) && item["contact_information"]?["email"] != null)
                        {
                            email = item["contact_information"]["email"].ToString();
                            _logger.LogDebug($"Found email in contact_information: {email}");
                        }

                        var business = new Business
                        {
                            BusinessId = item["business_id"]?.ToString(),
                            Name = item["name"]?.ToString(),
                            PhoneNumber = item["phone_number"]?.ToString(),
                            Email = email,
                            FullAddress = item["full_address"]?.ToString(),
                            Rating = item["rating"]?.Value<double?>() ?? 0,
                            ReviewCount = item["review_count"]?.Value<int?>() ?? 0,
                            Website = item["website"]?.ToString(),
                            OpeningStatus = TryGetNestedValue(item, "opening_hours", "open_now")?.ToString(),
                            PlaceLink = item["place_link"]?.ToString(),
                            ReviewsLink = item["reviews_link"]?.ToString(),
                            OwnerName = item["owner_name"]?.ToString(),
                            BusinessStatus = item["business_status"]?.ToString(),
                            Type = item["type"]?.ToString(),
                            PriceLevel = item["price_level"]?.ToString(),
                            StreetAddress = TryGetNestedValue(item, "address", "street_address")?.ToString(),
                            City = TryGetNestedValue(item, "address", "city")?.ToString(),
                            Zipcode = TryGetNestedValue(item, "address", "postal_code")?.ToString(),
                            State = TryGetNestedValue(item, "address", "state")?.ToString(),
                            Country = TryGetNestedValue(item, "address", "country")?.ToString()
                        };

                        _logger.LogInformation($"Successfully parsed business: {business.Name}");
                        _logger.LogDebug($"Business details - Email: {business.Email}, Phone: {business.PhoneNumber}");

                        businesses.Add(business);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error parsing individual business: {ex.Message}");
                        _logger.LogError($"Business data that caused error: {item.ToString()}");
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error parsing businesses: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                throw new Exception("Failed to parse business data", ex);
            }

            _logger.LogInformation($"Finished parsing {businesses.Count} businesses");
            return businesses;
        }

        private JToken? TryGetNestedValue(JToken token, params string[] path)
        {
            try
            {
                JToken? current = token;
                foreach (var segment in path)
                {
                    if (current == null) return null;
                    current = current[segment];
                }
                return current;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error getting nested value: {ex.Message}");
                return null;
            }
        }
    }
}
