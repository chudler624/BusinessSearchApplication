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

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://local-business-data.p.rapidapi.com/search?query={Uri.EscapeDataString(query)}%20in%20{zipcode}&limit={limit}&extract_emails_and_contacts=true"),
            };

            request.Headers.Add("x-rapidapi-key", _apiKey);
            request.Headers.Add("x-rapidapi-host", _apiHost);

            int retryCount = 0;
            const int maxRetries = 3;
            const int retryDelay = 2000; // 2 seconds

            while (retryCount < maxRetries)
            {
                try
                {
                    var response = await _httpClient.SendAsync(request);
                    response.EnsureSuccessStatusCode();
                    var body = await response.Content.ReadAsStringAsync();

                    businesses = ParseBusinesses(JObject.Parse(body));

                    var cacheEntryOptions = new MemoryCacheEntryOptions()
                        .SetSlidingExpiration(TimeSpan.FromMinutes(30));

                    _cache.Set(cacheKey, businesses, cacheEntryOptions);
                    _logger.LogInformation($"Cached data for key: {cacheKey}");

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
                    await Task.Delay(retryDelay * retryCount); // Exponential backoff
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error occurred while fetching business data: {ex.Message}");
                    throw;
                }
            }

            throw new Exception("Failed to retrieve data after multiple attempts");
        }

        private List<Business> ParseBusinesses(JObject apiResult)
        {
            var businesses = new List<Business>();
            var data = apiResult["data"] as JArray;

            if (data != null)
            {
                foreach (var item in data)
                {
                    businesses.Add(new Business
                    {
                        BusinessId = item["business_id"]?.ToString(),
                        Name = item["name"]?.ToString(),
                        PhoneNumber = item["phone_number"]?.ToString(),
                        FullAddress = item["full_address"]?.ToString(),
                        Rating = item["rating"]?.Value<double>() ?? 0,
                        ReviewCount = item["review_count"]?.Value<int>() ?? 0,
                        Website = item["website"]?.ToString(),
                    });
                }
            }

            return businesses;
        }
    }
}


 
