using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessSearch.Models.ViewModels;
using BusinessSearch.Services;
using BusinessSearch.Authorization;

namespace BusinessSearch.Controllers
{
    [Authorize]
    public class OrganizationController : Controller
    {
        private readonly IOrganizationService _organizationService;
        private readonly IOrganizationFilterService _orgFilter;
        private readonly IOrganizationInviteService _inviteService;
        private readonly ILogger<OrganizationController> _logger;

        public OrganizationController(
            IOrganizationService organizationService,
            IOrganizationFilterService orgFilter,
            IOrganizationInviteService inviteService,
            ILogger<OrganizationController> logger)
        {
            _organizationService = organizationService;
            _orgFilter = orgFilter;
            _inviteService = inviteService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var organization = await _orgFilter.GetCurrentOrganization();
                if (organization == null)
                {
                    return RedirectToAction("NoOrganization", "Account");
                }

                var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var isAdmin = await _organizationService.CanManageRoles(currentUserId, organization.Id);

                var users = await _organizationService.GetOrganizationUsersAsync(organization.Id);
                var permissions = await _organizationService.GetUserPermissionsAsync(organization.Id);
                var activeInvites = isAdmin ? await _inviteService.GetActiveInvites(organization.Id) : new List<Models.Organization.OrganizationInvite>();

                var viewModel = new OrganizationDetailsViewModel
                {
                    Id = organization.Id,
                    Name = organization.Name,
                    CreatedAt = organization.CreatedAt,
                    UserCount = users.Count(),
                    IsCurrentUserAdmin = isAdmin,
                    CurrentUserId = currentUserId,
                    Members = users.Select(u => new OrganizationMemberViewModel
                    {
                        Id = u.Id,
                        Name = u.Name,
                        Email = u.Email,
                        Role = u.OrganizationRole.ToString(),
                        JoinedDate = u.CreatedAt,
                        Permissions = permissions.FirstOrDefault(p => p.UserId == u.Id)
                    }).ToList(),
                    ActiveInvites = activeInvites.Select(i => new OrganizationInviteViewModel
                    {
                        InviteCode = i.InviteCode,
                        Role = i.Role,
                        ExpiryDate = i.ExpiryDate
                    }).ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading organization details: {ex.Message}");
                TempData["Error"] = "Error loading organization details.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            try
            {
                var organization = await _orgFilter.GetCurrentOrganization();
                if (organization == null)
                {
                    return RedirectToAction("NoOrganization", "Account");
                }

                var viewModel = new OrganizationSettingsViewModel
                {
                    Id = organization.Id,
                    Name = organization.Name,
                    CreatedAt = organization.CreatedAt,
                    CreatedByName = organization.CreatedBy?.Name
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading organization settings: {ex.Message}");
                TempData["Error"] = "Error loading organization settings.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSettings(OrganizationSettingsViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View("Settings", model);
                }

                var organization = await _orgFilter.GetCurrentOrganization();
                if (organization == null)
                {
                    return RedirectToAction("NoOrganization", "Account");
                }

                organization.Name = model.Name;
                // Add more settings as needed

                // Update logic would go here
                TempData["Success"] = "Organization settings updated successfully.";
                return RedirectToAction("Settings");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating organization settings: {ex.Message}");
                TempData["Error"] = "Error updating organization settings.";
                return View("Settings", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserRole(string userId, string role)
        {
            try
            {
                var organization = await _orgFilter.GetCurrentOrganization();
                var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (organization == null || currentUserId == null)
                {
                    return Json(new { success = false, message = "Organization not found" });
                }

                if (Enum.TryParse<Models.Organization.OrganizationRole>(role, out var organizationRole))
                {
                    var success = await _organizationService.UpdateUserRoleAsync(currentUserId, userId, organization.Id, organizationRole);
                    if (!success)
                    {
                        return Json(new { success = false, message = "You don't have permission to update roles" });
                    }
                    return Json(new { success = true });
                }

                return Json(new { success = false, message = "Invalid role specified" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating user role: {ex.Message}");
                return Json(new { success = false, message = "Error updating user role" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveUser(string userId)
        {
            try
            {
                var organization = await _orgFilter.GetCurrentOrganization();
                var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (organization == null || currentUserId == null)
                {
                    return Json(new { success = false, message = "Organization not found" });
                }

                var success = await _organizationService.RemoveUserFromOrganizationAsync(currentUserId, userId, organization.Id);
                if (!success)
                {
                    return Json(new { success = false, message = "You don't have permission to remove this user" });
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error removing user: {ex.Message}");
                return Json(new { success = false, message = "Error removing user" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateInvite(string role)
        {
            try
            {
                var organization = await _orgFilter.GetCurrentOrganization();
                var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (organization == null || currentUserId == null)
                {
                    return Json(new { success = false, message = "Organization not found" });
                }

                if (!await _organizationService.CanManageRoles(currentUserId, organization.Id))
                {
                    return Json(new { success = false, message = "You don't have permission to generate invites" });
                }

                var inviteCode = await _inviteService.GenerateInviteCode(organization.Id, role);
                return Json(new { success = true, inviteCode = inviteCode });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating invite code: {ex.Message}");
                return Json(new { success = false, message = "Error generating invite code" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateInvite(string inviteCode)
        {
            try
            {
                var organization = await _orgFilter.GetCurrentOrganization();
                var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (organization == null || currentUserId == null)
                {
                    return Json(new { success = false, message = "Organization not found" });
                }

                if (!await _organizationService.CanManageRoles(currentUserId, organization.Id))
                {
                    return Json(new { success = false, message = "You don't have permission to deactivate invites" });
                }

                var invite = await _inviteService.GetInviteDetails(inviteCode);
                if (invite?.OrganizationId != organization.Id)
                {
                    return Json(new { success = false, message = "Invalid invite code" });
                }

                await _inviteService.DeactivateInvite(inviteCode);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deactivating invite: {ex.Message}");
                return Json(new { success = false, message = "Error deactivating invite" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> JoinOrganization(string inviteCode)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not found" });
                }

                var result = await _inviteService.JoinOrganization(userId, inviteCode);
                if (result)
                {
                    return Json(new { success = true });
                }

                return Json(new { success = false, message = "Invalid or expired invite code" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error joining organization: {ex.Message}");
                return Json(new { success = false, message = "Error joining organization" });
            }
        }
    }
}