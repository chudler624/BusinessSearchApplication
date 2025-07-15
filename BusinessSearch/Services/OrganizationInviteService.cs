using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessSearch.Data;
using BusinessSearch.Models.Organization;
using Microsoft.EntityFrameworkCore;

namespace BusinessSearch.Services
{
    public class OrganizationInviteService : IOrganizationInviteService
    {
        private readonly ApplicationDbContext _context;
        private readonly IOrganizationService _organizationService;

        public OrganizationInviteService(ApplicationDbContext context, IOrganizationService organizationService)
        {
            _context = context;
            _organizationService = organizationService;
        }

        public async Task<string> GenerateInviteCode(int organizationId, string role, int validityDays = 7)
        {
            var invite = new OrganizationInvite
            {
                InviteCode = GenerateUniqueCode(),
                OrganizationId = organizationId,
                CreatedDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddDays(validityDays),
                IsActive = true,
                Role = role
            };

            _context.OrganizationInvites.Add(invite);
            await _context.SaveChangesAsync();

            return invite.InviteCode;
        }

        private string GenerateUniqueCode()
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var code = new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            while (_context.OrganizationInvites.Any(i => i.InviteCode == code))
            {
                code = new string(Enumerable.Repeat(chars, 8)
                    .Select(s => s[random.Next(s.Length)]).ToArray());
            }

            return code;
        }

        public async Task<bool> ValidateInviteCode(string inviteCode)
        {
            var invite = await _context.OrganizationInvites
                .FirstOrDefaultAsync(i => i.InviteCode == inviteCode);

            return invite != null &&
                   invite.IsActive &&
                   DateTime.UtcNow <= invite.ExpiryDate;
        }

        public async Task<OrganizationInvite> GetInviteDetails(string inviteCode)
        {
            return await _context.OrganizationInvites
                .Include(i => i.Organization)
                .FirstOrDefaultAsync(i => i.InviteCode == inviteCode);
        }

        public async Task<bool> JoinOrganization(string userId, string inviteCode)
        {
            var invite = await GetInviteDetails(inviteCode);

            if (invite == null || !invite.IsActive || DateTime.UtcNow > invite.ExpiryDate)
                return false;

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            await _organizationService.AddUserToOrganizationAsync(user, invite.OrganizationId, Enum.Parse<OrganizationRole>(invite.Role));

            invite.IsActive = false;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<OrganizationInvite>> GetActiveInvites(int organizationId)
        {
            return await _context.OrganizationInvites
                .Where(i => i.OrganizationId == organizationId &&
                           i.IsActive &&
                           i.ExpiryDate > DateTime.UtcNow)
                .ToListAsync();
        }

        public async Task<bool> DeactivateInvite(string inviteCode)
        {
            var invite = await _context.OrganizationInvites
                .FirstOrDefaultAsync(i => i.InviteCode == inviteCode);

            if (invite == null)
                return false;

            invite.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}