using BusinessSearch.Models;
using BusinessSearch.Models.ViewModels;
using BusinessSearch.Services;
using Microsoft.AspNetCore.Mvc;

namespace BusinessSearch.Controllers
{
    public class TeamController : Controller
    {
        private readonly TeamService _teamService;
        private readonly ILogger<TeamController> _logger;

        public TeamController(TeamService teamService, ILogger<TeamController> logger)
        {
            _teamService = teamService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var teamMembers = await _teamService.GetAllTeamMembers();
                var viewModel = new TeamManagementViewModel
                {
                    TeamMembers = teamMembers,
                    NewTeamMember = new TeamMember()
                };
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Team Index: {ex.Message}");
                TempData["Error"] = "An error occurred while loading team members.";
                return View(new TeamManagementViewModel());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(TeamMember teamMember)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    await _teamService.AddTeamMember(teamMember);
                    return Json(new { success = true });
                }
                return Json(new { success = false, message = "Invalid team member data." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding team member: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while adding the team member." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(TeamMember teamMember)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    await _teamService.UpdateTeamMember(teamMember);
                    return Json(new { success = true });
                }
                return Json(new { success = false, message = "Invalid team member data." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating team member: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while updating the team member." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _teamService.DeleteTeamMember(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting team member: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while deleting the team member." });
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var teamMember = await _teamService.GetTeamMemberById(id);
                if (teamMember == null)
                {
                    return NotFound();
                }

                var assignedLists = await _teamService.GetAssignedLists(id);
                var viewModel = new TeamMemberViewModel
                {
                    TeamMember = teamMember,
                    AssignedLists = assignedLists
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting team member details: {ex.Message}");
                TempData["Error"] = "An error occurred while loading team member details.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTeamMembers()
        {
            try
            {
                var members = await _teamService.GetAllTeamMembers();
                return Json(new { success = true, teamMembers = members });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching team members: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
