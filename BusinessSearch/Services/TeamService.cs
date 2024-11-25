using BusinessSearch.Data;
using BusinessSearch.Models;
using Microsoft.EntityFrameworkCore;

namespace BusinessSearch.Services
{
    public class TeamService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TeamService> _logger;

        public TeamService(ApplicationDbContext context, ILogger<TeamService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<TeamMember>> GetAllTeamMembers()
        {
            try
            {
                return await _context.TeamMembers
                    .Include(t => t.AssignedLists)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting team members: {ex.Message}");
                throw;
            }
        }

        public async Task<TeamMember?> GetTeamMemberById(int id)
        {
            try
            {
                return await _context.TeamMembers
                    .Include(t => t.AssignedLists)
                    .FirstOrDefaultAsync(t => t.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting team member {id}: {ex.Message}");
                throw;
            }
        }

        public async Task<TeamMember> AddTeamMember(TeamMember member)
        {
            try
            {
                _context.TeamMembers.Add(member);
                await _context.SaveChangesAsync();
                return member;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding team member: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateTeamMember(TeamMember member)
        {
            try
            {
                _context.Entry(member).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating team member: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteTeamMember(int id)
        {
            try
            {
                var member = await _context.TeamMembers.FindAsync(id);
                if (member != null)
                {
                    _context.TeamMembers.Remove(member);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting team member {id}: {ex.Message}");
                throw;
            }
        }

        public async Task<List<CrmList>> GetAssignedLists(int teamMemberId)
        {
            try
            {
                return await _context.CrmLists
                    .Where(l => l.AssignedToId == teamMemberId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting assigned lists for team member {teamMemberId}: {ex.Message}");
                throw;
            }
        }
    }
}
