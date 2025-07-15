using BusinessSearch.Models.Organization;

namespace BusinessSearch.Services
{
    public interface IOrganizationFilterService
    {
        IQueryable<T> ApplyOrganizationFilter<T>(IQueryable<T> query) where T : class;
        Task<int> GetCurrentOrganizationId();
        Task<bool> IsUserInOrganization(int organizationId);
        Task<OrganizationEntity> GetCurrentOrganization();
    }
}