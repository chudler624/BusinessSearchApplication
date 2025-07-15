using BusinessSearch.Data;
using BusinessSearch.Models;
using Microsoft.EntityFrameworkCore;

namespace BusinessSearch.Services
{
    public class TeamService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TeamService> _logger;
        private readonly IOrganizationFilterService _orgFilter;

        public TeamService(
            ApplicationDbContext context,
            ILogger<TeamService> logger,
            IOrganizationFilterService orgFilter)
        {
            _context = context;
            _logger = logger;
            _orgFilter = orgFilter;
        }

        public async Task<List<TeamMember>> GetAllTeamMembers()
        {
            try
            {
                var orgId = await _orgFilter.GetCurrentOrganizationId();
                return await _context.TeamMembers
                    .Join(_context.Users,
                        tm => tm.Id,
                        u => u.TeamMemberId,
                        (tm, u) => new { TeamMember = tm, User = u })
                    .Where(x => x.User.OrganizationId == orgId)
                    .Select(x => x.TeamMember)
                    .OrderBy(t => t.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving team members");
                throw;
            }
        }

        public async Task<TeamMember> GetTeamMemberById(int id)
        {
            try
            {
                var orgId = await _orgFilter.GetCurrentOrganizationId();
                return await _context.TeamMembers
                    .Join(_context.Users,
                        tm => tm.Id,
                        u => u.TeamMemberId,
                        (tm, u) => new { TeamMember = tm, User = u })
                    .Where(x => x.User.OrganizationId == orgId && x.TeamMember.Id == id)
                    .Select(x => x.TeamMember)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving team member with ID: {Id}", id);
                throw;
            }
        }

        public async Task<TeamMember> AddTeamMember(TeamMember teamMember)
        {
            try
            {
                teamMember.DateAdded = DateTime.UtcNow;
                teamMember.OrganizationId = await _orgFilter.GetCurrentOrganizationId();
                _context.TeamMembers.Add(teamMember);
                await _context.SaveChangesAsync();
                return teamMember;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding team member: {Name}", teamMember.Name);
                throw;
            }
        }

        public async Task<TeamMember> UpdateTeamMember(TeamMember teamMember)
        {
            try
            {
                var orgId = await _orgFilter.GetCurrentOrganizationId();
                var existingMember = await _context.TeamMembers
                    .Join(_context.Users,
                        tm => tm.Id,
                        u => u.TeamMemberId,
                        (tm, u) => new { TeamMember = tm, User = u })
                    .Where(x => x.User.OrganizationId == orgId && x.TeamMember.Id == teamMember.Id)
                    .Select(x => x.TeamMember)
                    .FirstOrDefaultAsync();

                if (existingMember == null)
                {
                    throw new InvalidOperationException($"TeamMember with ID {teamMember.Id} not found or access denied");
                }

                _context.Entry(existingMember).CurrentValues.SetValues(teamMember);
                await _context.SaveChangesAsync();
                return existingMember;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating team member with ID: {Id}", teamMember.Id);
                throw;
            }
        }

        public async Task<bool> DeleteTeamMember(int id)
        {
            try
            {
                var orgId = await _orgFilter.GetCurrentOrganizationId();
                var teamMember = await _context.TeamMembers
                    .Join(_context.Users,
                        tm => tm.Id,
                        u => u.TeamMemberId,
                        (tm, u) => new { TeamMember = tm, User = u })
                    .Where(x => x.User.OrganizationId == orgId && x.TeamMember.Id == id)
                    .Select(x => x.TeamMember)
                    .FirstOrDefaultAsync();

                if (teamMember == null)
                {
                    return false;
                }

                _context.TeamMembers.Remove(teamMember);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting team member with ID: {Id}", id);
                throw;
            }
        }

        public async Task<List<CrmList>> GetAssignedLists(int teamMemberId)
        {
            try
            {
                var orgId = await _orgFilter.GetCurrentOrganizationId();
                return await _context.CrmLists
                    .Where(l => l.AssignedToId == teamMemberId.ToString() && l.OrganizationId == orgId)
                    .Include(l => l.CrmEntryLists)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving assigned lists for team member ID: {Id}", teamMemberId);
                throw;
            }
        }

        public async Task<bool> ValidateTeamMemberId(int? id)
        {
            if (!id.HasValue) return false;
            var orgId = await _orgFilter.GetCurrentOrganizationId();
            return await _context.TeamMembers
                .Join(_context.Users,
                    tm => tm.Id,
                    u => u.TeamMemberId,
                    (tm, u) => new { TeamMember = tm, User = u })
                .AnyAsync(x => x.User.OrganizationId == orgId && x.TeamMember.Id == id.Value);
        }
    }
}