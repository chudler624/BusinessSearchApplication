using System.Threading.Tasks;
using BusinessSearch.Models.WebsiteAnalysis;

namespace BusinessSearch.Services.WebsiteOpportunitiesServices.Interfaces
{
    public interface IPageSpeedService
    {
        Task<PageSpeedResult> AnalyzePageSpeed(string content, double ttfb, double totalLoadTime);
    }
}