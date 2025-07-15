using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessSearch.Models.Organization;

namespace BusinessSearch.Services
{
    public interface IOrganizationInviteService
    {
        Task<string> GenerateInviteCode(int organizationId, string role, int validityDays = 7);
        Task<bool> ValidateInviteCode(string inviteCode);
        Task<OrganizationInvite> GetInviteDetails(string inviteCode);
        Task<bool> JoinOrganization(string userId, string inviteCode);
        Task<IEnumerable<OrganizationInvite>> GetActiveInvites(int organizationId);
        Task<bool> DeactivateInvite(string inviteCode);
    }
}