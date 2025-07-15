using System;
using System.Threading.Tasks;
using BusinessSearch.Models.ViewModels;

namespace BusinessSearch.Services
{
    public interface ISearchUsageService
    {
        Task<SearchUsageStatus> GetSearchUsageStatusAsync(int organizationId);
        Task<bool> CanPerformSearchAsync(int organizationId, int requestedResults = 1);
        Task IncrementSearchResultsCountAsync(int organizationId, int actualResultsReturned);
        Task ResetDailySearchCountsAsync();
    }
}