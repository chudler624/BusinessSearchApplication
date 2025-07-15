using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;
using BusinessSearch.Models.WebsiteAnalysis;
using BusinessSearch.Services.WebsiteOpportunitiesServices.Interfaces;

namespace BusinessSearch.Services.WebsiteOpportunitiesServices
{
    public class WebsiteOpportunitiesService : IWebsiteOpportunitiesService
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly IResponsivenessService _responsivenessService;
        private readonly IGdprComplianceService _gdprComplianceService;
        private readonly IPageSpeedService _pageSpeedService;
        private readonly ILocalSeoService _localSeoService;

        public WebsiteOpportunitiesService(
            IHttpClientFactory clientFactory,
            IResponsivenessService responsivenessService,
            IGdprComplianceService gdprComplianceService,
            IPageSpeedService pageSpeedService,
            ILocalSeoService localSeoService)
        {
            _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
            _responsivenessService = responsivenessService ?? throw new ArgumentNullException(nameof(responsivenessService));
            _gdprComplianceService = gdprComplianceService ?? throw new ArgumentNullException(nameof(gdprComplianceService));
            _pageSpeedService = pageSpeedService ?? throw new ArgumentNullException(nameof(pageSpeedService));
            _localSeoService = localSeoService ?? throw new ArgumentNullException(nameof(localSeoService));
        }

        public async Task<WebsiteAnalysisModel> AnalyzeWebsite(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException("URL cannot be null or empty", nameof(url));
            }

            var client = _clientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

            var stopwatch = Stopwatch.StartNew();
            var response = await client.GetAsync(url);
            var ttfb = stopwatch.ElapsedMilliseconds;

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to fetch the website. Status code: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            stopwatch.Stop();

            var responsivenessResult = _responsivenessService.AnalyzeResponsiveness(content);
            var gdprResult = _gdprComplianceService.AnalyzeGdprCompliance(content);
            var pageSpeedResult = await _pageSpeedService.AnalyzePageSpeed(content, ttfb, stopwatch.ElapsedMilliseconds);
            var localSeoResult = await _localSeoService.AnalyzeLocalSeo(content);

            return new WebsiteAnalysisModel
            {
                ResponsivenessResult = responsivenessResult,
                GdprComplianceResult = gdprResult,
                PageSpeedResult = pageSpeedResult,
                LocalSeoResult = localSeoResult
            };
        }
    }
}