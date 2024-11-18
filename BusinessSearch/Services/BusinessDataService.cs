using BusinessSearch.Models;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;

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
            Console.WriteLine("BusinessDataService initialized");
        }

        public async Task<List<Business>> SearchBusinessesAsync(string query, string zipcode, int? limit = 5)
        {
            try
            {
                Console.WriteLine($"\n=== Starting Business Search ===");
                Console.WriteLine($"Query: {query}, Zipcode: {zipcode}, Limit: {limit}");

                var actualLimit = limit == 100 ? 50 : limit ?? 5;
                var cacheKey = $"search_{query}_{zipcode}_{actualLimit}";

                Console.WriteLine($"Cache key: {cacheKey}");

                if (_cache.TryGetValue(cacheKey, out List<Business>? businesses))
                {
                    Console.WriteLine($"Cache hit - returning {businesses?.Count ?? 0} cached results");
                    return businesses;
                }

                Console.WriteLine("Cache miss - making API request");

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(
                        $"https://local-business-data.p.rapidapi.com/search?" +
                        $"query={Uri.EscapeDataString(query)}%20in%20{zipcode}" +
                        $"&limit={actualLimit}" +
                        $"&zoom=13" +
                        $"&language=en" +
                        $"&region=us" +
                        $"&extract_emails_and_contacts=true"
                    )
                };

                Console.WriteLine($"API Request URL: {request.RequestUri}");
                request.Headers.Add("x-rapidapi-key", _apiKey);
                request.Headers.Add("x-rapidapi-host", _apiHost);

                var response = await _httpClient.SendAsync(request);
                Console.WriteLine($"API Response Status: {response.StatusCode}");

                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API Response Length: {body.Length} characters");
                Console.WriteLine("Raw API Response:");
                Console.WriteLine(body);

                try
                {
                    var jsonObject = JObject.Parse(body);
                    businesses = ParseBusinesses(jsonObject);

                    var cacheEntryOptions = new MemoryCacheEntryOptions()
                        .SetSlidingExpiration(TimeSpan.FromMinutes(30));

                    _cache.Set(cacheKey, businesses, cacheEntryOptions);
                    Console.WriteLine($"Cached {businesses.Count} businesses");

                    return businesses;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"JSON parsing error: {ex.Message}");
                    Console.WriteLine($"Raw response that caused error: {body}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Search error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private List<Business> ParseBusinesses(JObject apiResult)
        {
            var businesses = new List<Business>();
            var skippedBusinesses = new List<string>();

            try
            {
                Console.WriteLine("\n=== Parsing Businesses ===");
                var data = apiResult["data"];
                if (data == null)
                {
                    Console.WriteLine("WARNING: No 'data' field in API response");
                    Console.WriteLine($"Raw API response: {apiResult}");
                    return businesses;
                }

                // Try to parse as array
                if (!(data is JArray dataArray))
                {
                    Console.WriteLine($"WARNING: 'data' field is not an array. Type: {data.Type}");
                    Console.WriteLine($"Data content: {data}");
                    return businesses;
                }

                Console.WriteLine($"Found {dataArray.Count} businesses in API response");

                foreach (JToken item in dataArray)
                {
                    try
                    {
                        Console.WriteLine($"\nParsing business raw data:");
                        Console.WriteLine(item.ToString());

                        var businessName = item["name"]?.ToString() ?? "Unknown";
                        Console.WriteLine($"Processing business: {businessName}");

                        var emailToken = item["emails_and_contacts"]?["emails"]?.FirstOrDefault();
                        var email = emailToken?.ToString();
                        var phone = item["phone_number"]?.ToString();

                        Console.WriteLine($"Basic info - Name: {businessName}, Email: {email}, Phone: {phone}");

                        var business = new Business
                        {
                            BusinessId = item["business_id"]?.ToString(),
                            Name = businessName,
                            PhoneNumber = phone,
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
                            Country = TryGetNestedValue(item, "address", "country")?.ToString(),
                            PhotoUrl = item["photos_sample"]?[0]?["photo_url"]?.ToString(),
                            Facebook = item["emails_and_contacts"]?["facebook"]?.ToString(),
                            Instagram = item["emails_and_contacts"]?["instagram"]?.ToString(),
                            YelpUrl = item["emails_and_contacts"]?["yelp"]?.ToString()
                        };

                        businesses.Add(business);
                        Console.WriteLine($"Successfully parsed business: {businessName}");
                    }
                    catch (Exception ex)
                    {
                        var businessName = item["name"]?.ToString() ?? "Unknown";
                        skippedBusinesses.Add(businessName);
                        Console.WriteLine($"Error parsing business {businessName}: {ex.Message}");
                        Console.WriteLine($"Error stack trace: {ex.StackTrace}");
                        Console.WriteLine($"Problematic JSON: {item}");
                        continue;
                    }
                }

                Console.WriteLine($"\n=== Parsing Summary ===");
                Console.WriteLine($"Successfully parsed {businesses.Count} businesses");
                if (skippedBusinesses.Any())
                {
                    Console.WriteLine($"Skipped businesses: {string.Join(", ", skippedBusinesses)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Critical error parsing businesses: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Console.WriteLine($"Full API response: {apiResult}");
            }

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
                Console.WriteLine($"Error getting nested value: {ex.Message}");
                return null;
            }
        }
    }
}