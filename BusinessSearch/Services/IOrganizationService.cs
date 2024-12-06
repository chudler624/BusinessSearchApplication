using BusinessSearch.Models;
using BusinessSearch.Models.Organization;

namespace BusinessSearch.Services
{
    public interface IOrganizationService
    {
        Task<OrganizationEntity> CreateOrganizationAsync(string name, ApplicationUser owner);
        Task<bool> AddUserToOrganizationAsync(ApplicationUser user, int organizationId, OrganizationRole role);
        Task<OrganizationPermissions> SetUserPermissionsAsync(string userId, bool canSearch, bool canViewHistory, bool canManageCrm);
        Task<bool> IsUserAuthorizedForOrganizationAsync(string userId, int organizationId);
        Task<OrganizationEntity> GetOrganizationByIdAsync(int organizationId);
        Task<List<ApplicationUser>> GetOrganizationUsersAsync(int organizationId);
        Task<List<OrganizationPermissions>> GetUserPermissionsAsync(int organizationId);
        Task<OrganizationEntity> GetUserOrganizationAsync(string userId);
        Task<bool> CanManageUser(string currentUserId, string targetUserId, int organizationId);
        Task<bool> CanManageRoles(string currentUserId, int organizationId);
        Task<bool> RemoveUserFromOrganizationAsync(string currentUserId, string targetUserId, int organizationId);
        Task<bool> UpdateUserRoleAsync(string currentUserId, string targetUserId, int organizationId, OrganizationRole newRole);
        Task<bool> UpdateOrganizationPlanAsync(int organizationId, OrganizationPlan newPlan, string promoCode);
        Task<bool> UpdateOrganizationAsync(OrganizationEntity organization);
    }
}