using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BusinessSearch.Data;
using BusinessSearch.Models;
using BusinessSearch.Models.Organization;

namespace BusinessSearch.Services
{
    public class OrganizationFilterService : IOrganizationFilterService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private int? _cachedOrganizationId;

        public OrganizationFilterService(
            IHttpContextAccessor httpContextAccessor,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
            _userManager = userManager;
        }

        public async Task<int> GetCurrentOrganizationId()
        {
            if (_cachedOrganizationId.HasValue)
                return _cachedOrganizationId.Value;

            var user = await GetCurrentUser();
            _cachedOrganizationId = user?.OrganizationId ?? -1;
            return _cachedOrganizationId.Value;
        }

        public async Task<OrganizationEntity> GetCurrentOrganization()
        {
            var user = await GetCurrentUser();
            if (user?.OrganizationId == null) return null;

            return await _context.Organizations
                .Include(o => o.CreatedBy)
                .Include(o => o.Users)
                .Include(o => o.CrmLists)
                .Include(o => o.SavedSearches)
                .FirstOrDefaultAsync(o => o.Id == user.OrganizationId);
        }

        public async Task<bool> IsUserInOrganization(int organizationId)
        {
            var user = await GetCurrentUser();
            return user?.OrganizationId == organizationId;
        }

        public IQueryable<T> ApplyOrganizationFilter<T>(IQueryable<T> query) where T : class
        {
            // Using Task.Run to avoid deadlocks while still maintaining async context
            var organizationId = Task.Run(() => GetCurrentOrganizationId()).Result;
            if (organizationId == -1) return query.Take(0);

            var entityType = typeof(T);

            if (typeof(CrmEntry).IsAssignableFrom(entityType))
            {
                return query.Cast<CrmEntry>()
                    .Where(e => e.CrmEntryLists.Any(el => el.CrmList.OrganizationId == organizationId))
                    .Cast<T>();
            }

            if (typeof(CrmList).IsAssignableFrom(entityType))
            {
                return query.Cast<CrmList>()
                    .Where(l => l.OrganizationId == organizationId)
                    .Cast<T>();
            }

            if (typeof(SavedSearch).IsAssignableFrom(entityType))
            {
                return query.Cast<SavedSearch>()
                    .Where(s => s.OrganizationId == organizationId)
                    .Cast<T>();
            }

            return query;
        }

        private async Task<ApplicationUser> GetCurrentUser()
        {
            if (_httpContextAccessor.HttpContext?.User == null) return null;

            var userId = _userManager.GetUserId(_httpContextAccessor.HttpContext.User);
            if (userId == null) return null;

            return await _userManager.Users
                .Include(u => u.Organization)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }
    }
}