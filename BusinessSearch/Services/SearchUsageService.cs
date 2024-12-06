using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using BusinessSearch.Data;
using BusinessSearch.Models.Organization;
using BusinessSearch.Models.ViewModels;

namespace BusinessSearch.Services
{
    public class SearchUsageService : ISearchUsageService, IHostedService
    {
        private readonly ILogger<SearchUsageService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private Timer _timer;

        public SearchUsageService(IServiceProvider serviceProvider, ILogger<SearchUsageService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task<SearchUsageStatus> GetSearchUsageStatusAsync(int organizationId)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var org = await context.Organizations
                .Include(o => o.SearchUsage)
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (org == null)
                throw new ArgumentException($"Organization not found: {organizationId}");

            var dailyLimit = org.Plan.GetDailySearchLimit();
            var todayUsage = await context.OrganizationSearchUsage
                .Where(u => u.OrganizationId == organizationId && u.Date == DateTime.UtcNow.Date)
                .SumAsync(u => u.ResultsCount);

            return new SearchUsageStatus
            {
                DailyLimit = dailyLimit,
                UsedToday = todayUsage,
                NextReset = org.NextSearchReset
            };
        }

        public async Task<bool> CanPerformSearchAsync(int organizationId, int requestedResults = 1)
        {
            var status = await GetSearchUsageStatusAsync(organizationId);
            return status.Remaining >= requestedResults;
        }

        public async Task IncrementSearchResultsCountAsync(int organizationId, int actualResultsReturned)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (!await CanPerformSearchAsync(organizationId, actualResultsReturned))
                throw new InvalidOperationException("Search limit exceeded");

            var today = DateTime.UtcNow.Date;
            var usage = await context.OrganizationSearchUsage
                .FirstOrDefaultAsync(u => u.OrganizationId == organizationId && u.Date == today);

            if (usage == null)
            {
                usage = new OrganizationSearchUsage
                {
                    OrganizationId = organizationId,
                    Date = today,
                    ResultsCount = actualResultsReturned,
                    Count = 1,
                    LastUpdated = DateTime.UtcNow
                };
                context.OrganizationSearchUsage.Add(usage);
            }
            else
            {
                usage.ResultsCount += actualResultsReturned;
                usage.Count++;
                usage.LastUpdated = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();
            _logger.LogInformation("Incremented search results count by {ResultsCount} for organization {OrganizationId}",
                actualResultsReturned, organizationId);
        }

        public async Task ResetDailySearchCountsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var today = DateTime.UtcNow.Date;

            // Update NextSearchReset for all organizations
            await context.Organizations
                .Where(o => o.NextSearchReset <= today)
                .ForEachAsync(org =>
                {
                    org.NextSearchReset = today.AddDays(1);
                });

            // Create new usage records for the new day
            var organizations = await context.Organizations.ToListAsync();
            foreach (var org in organizations)
            {
                context.OrganizationSearchUsage.Add(new OrganizationSearchUsage
                {
                    OrganizationId = org.Id,
                    Date = today,
                    Count = 0,
                    ResultsCount = 0,
                    LastUpdated = DateTime.UtcNow
                });
            }

            await context.SaveChangesAsync();
            _logger.LogInformation("Reset daily search counts for {Count} organizations", organizations.Count);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Search Reset Service is starting.");
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Search Reset Service is stopping.");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            var now = DateTime.UtcNow;
            if (now.Hour == 0 && now.Minute == 0)
            {
                ResetDailySearchCountsAsync().Wait();
            }
        }
    }
}