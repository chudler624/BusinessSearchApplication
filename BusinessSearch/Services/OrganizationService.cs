using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;
using BusinessSearch.Data;
using BusinessSearch.Models;
using BusinessSearch.Models.Organization;

namespace BusinessSearch.Services
{
    public class OrganizationService : IOrganizationService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<OrganizationService> _logger;

        public OrganizationService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,
            ILogger<OrganizationService> logger)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task<bool> CanManageUser(string currentUserId, string targetUserId, int organizationId)
        {
            var currentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == currentUserId && u.OrganizationId == organizationId);

            if (currentUser == null)
                return false;

            // Allow users to remove themselves
            if (currentUserId == targetUserId)
                return true;

            // Only admins can remove other users
            return currentUser.OrganizationRole == OrganizationRole.Admin;
        }

        public async Task<bool> CanManageRoles(string currentUserId, int organizationId)
        {
            var currentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == currentUserId && u.OrganizationId == organizationId);

            return currentUser?.OrganizationRole == OrganizationRole.Admin;
        }

        public async Task<OrganizationEntity> CreateOrganizationAsync(string name, ApplicationUser owner)
        {
            try
            {
                _logger.LogInformation($"Creating organization: {name} for user: {owner.Id}");

                var organization = new OrganizationEntity
                {
                    Name = name,
                    CreatedAt = DateTime.UtcNow,
                    CreatedById = owner.Id,
                    Plan = OrganizationPlan.Bronze,
                    NextSearchReset = DateTime.UtcNow.Date.AddDays(1)
                };

                var strategy = _context.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        _context.Organizations.Add(organization);
                        await _context.SaveChangesAsync();

                        owner.OrganizationId = organization.Id;
                        owner.OrganizationRole = OrganizationRole.Admin;
                        _context.Users.Update(owner);
                        await _context.SaveChangesAsync();

                        await transaction.CommitAsync();
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                });

                return organization;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in CreateOrganizationAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> AddUserToOrganizationAsync(ApplicationUser user, int organizationId, OrganizationRole role)
        {
            user.OrganizationId = organizationId;
            user.OrganizationRole = role;

            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task<OrganizationPermissions> SetUserPermissionsAsync(
            string userId,
            bool canSearch,
            bool canViewHistory,
            bool canManageCrm)
        {
            var permissions = await _context.OrganizationPermissions
                .FirstOrDefaultAsync(p => p.UserId == userId)
                ?? new OrganizationPermissions { UserId = userId };

            permissions.CanSearch = canSearch;
            permissions.CanViewHistory = canViewHistory;
            permissions.CanManageCrm = canManageCrm;

            if (permissions.Id == 0)
                _context.OrganizationPermissions.Add(permissions);

            await _context.SaveChangesAsync();
            return permissions;
        }

        public async Task<bool> IsUserAuthorizedForOrganizationAsync(string userId, int organizationId)
        {
            return await _context.Users
                .AnyAsync(u => u.Id == userId && u.OrganizationId == organizationId);
        }

        public async Task<OrganizationEntity> GetOrganizationByIdAsync(int organizationId)
        {
            return await _context.Organizations
                .Include(o => o.Users)
                .FirstOrDefaultAsync(o => o.Id == organizationId);
        }

        public async Task<List<ApplicationUser>> GetOrganizationUsersAsync(int organizationId)
        {
            return await _context.Users
                .Where(u => u.OrganizationId == organizationId)
                .ToListAsync();
        }

        public async Task<List<OrganizationPermissions>> GetUserPermissionsAsync(int organizationId)
        {
            var users = await GetOrganizationUsersAsync(organizationId);
            var userIds = users.Select(u => u.Id).ToList();

            return await _context.OrganizationPermissions
                .Where(p => userIds.Contains(p.UserId))
                .ToListAsync();
        }

        public async Task<bool> RemoveUserFromOrganizationAsync(string currentUserId, string targetUserId, int organizationId)
        {
            if (!await CanManageUser(currentUserId, targetUserId, organizationId))
                return false;

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == targetUserId && u.OrganizationId == organizationId);

            if (user == null) return false;

            user.OrganizationId = null;
            user.OrganizationRole = null;

            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> UpdateUserRoleAsync(string currentUserId, string targetUserId, int organizationId, OrganizationRole newRole)
        {
            if (!await CanManageRoles(currentUserId, organizationId))
                return false;

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == targetUserId && u.OrganizationId == organizationId);

            if (user == null) return false;

            user.OrganizationRole = newRole;
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task<OrganizationEntity> GetUserOrganizationAsync(string userId)
        {
            var user = await _context.Users
                .Include(u => u.Organization)
                .FirstOrDefaultAsync(u => u.Id == userId);

            return user?.Organization;
        }

        public async Task<bool> UpdateOrganizationPlanAsync(int organizationId, OrganizationPlan newPlan, string promoCode)
        {
            var organization = await _context.Organizations.FindAsync(organizationId);
            if (organization == null)
                return false;

            // Validate promo code for unlimited plan
            if (newPlan == OrganizationPlan.Unlimited &&
                (!string.Equals(promoCode, "PATRIOT", StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            organization.Plan = newPlan;
            organization.PromoCode = promoCode;

            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> UpdateOrganizationAsync(OrganizationEntity organization)
        {
            _context.Organizations.Update(organization);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }
    }
}