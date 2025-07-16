using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessSearch.Models.ViewModels;
using BusinessSearch.Services;
using BusinessSearch.Authorization;
using BusinessSearch.Models.Organization;

namespace BusinessSearch.Controllers
{
    [Authorize]
    public class OrganizationController : Controller
    {
        private readonly IOrganizationService _organizationService;
        private readonly IOrganizationFilterService _orgFilter;
        private readonly IOrganizationInviteService _inviteService;
        private readonly ISearchUsageService _searchUsageService;
        private readonly ILogger<OrganizationController> _logger;

        public OrganizationController(
            IOrganizationService organizationService,
            IOrganizationFilterService orgFilter,
            IOrganizationInviteService inviteService,
            ISearchUsageService searchUsageService,
            ILogger<OrganizationController> logger)
        {
            _organizationService = organizationService;
            _orgFilter = orgFilter;
            _inviteService = inviteService;
            _searchUsageService = searchUsageService;
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
                    _logger.LogWarning("No organization found for current user");
                    return RedirectToAction("NoOrganization", "Account");
                }

                _logger.LogInformation($"Loading organization details for organization ID: {organization.Id}");

                var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (currentUserId == null)
                {
                    _logger.LogWarning("Current user ID not found");
                    return RedirectToAction("Login", "Account");
                }

                var searchUsage = await _searchUsageService.GetSearchUsageStatusAsync(organization.Id);
                var isAdmin = await _organizationService.CanManageRoles(currentUserId, organization.Id);
                var users = await _organizationService.GetOrganizationUsersAsync(organization.Id);
                var permissions = await _organizationService.GetUserPermissionsAsync(organization.Id);
                var activeInvites = isAdmin ? await _inviteService.GetActiveInvites(organization.Id) : new List<OrganizationInvite>();

                var viewModel = new OrganizationDetailsViewModel
                {
                    Id = organization.Id,
                    Name = organization.Name,
                    CreatedAt = organization.CreatedAt,
                    UserCount = users.Count(),
                    IsCurrentUserAdmin = isAdmin,
                    CurrentUserId = currentUserId,
                    Plan = organization.Plan,
                    SearchUsage = searchUsage,
                    Members = users.Select(u => new OrganizationMemberViewModel
                    {
                        Id = u.Id,
                        Name = u.Name ?? string.Empty,
                        Email = u.Email ?? string.Empty,
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
                _logger.LogError(ex, "Error loading organization details: {Message}", ex.Message);
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
                    CreatedByName = organization.CreatedBy?.Name,
                    Plan = organization.Plan,
                    PromoCode = organization.PromoCode
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

                if (organization.Plan != model.Plan || organization.PromoCode != model.PromoCode)
                {
                    var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (!string.IsNullOrEmpty(currentUserId) && await _organizationService.CanManageRoles(currentUserId, organization.Id))
                    {
                        var updateSuccess = await _organizationService.UpdateOrganizationPlanAsync(
                            organization.Id,
                            model.Plan,
                            model.PromoCode);

                        if (!updateSuccess)
                        {
                            ModelState.AddModelError("", "Unable to update organization plan. Please check your promo code if you're upgrading to unlimited.");
                            return View("Settings", model);
                        }
                    }
                }

                await _organizationService.UpdateOrganizationAsync(organization);
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

                if (Enum.TryParse<OrganizationRole>(role, out var organizationRole))
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignUserToRole(string userId, string role)
        {
            try
            {
                var organization = await _orgFilter.GetCurrentOrganization();
                var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (organization == null || currentUserId == null)
                {
                    return Json(new { success = false, message = "Organization not found" });
                }

                // Only Admins can assign roles
                if (!await _organizationService.CanManageRoles(currentUserId, organization.Id))
                {
                    return Json(new { success = false, message = "You don't have permission to assign roles" });
                }

                if (Enum.TryParse<OrganizationRole>(role, out var organizationRole))
                {
                    var success = await _organizationService.UpdateUserRoleAsync(currentUserId, userId, organization.Id, organizationRole);
                    if (!success)
                    {
                        return Json(new { success = false, message = "Failed to update user role" });
                    }
                    return Json(new { success = true });
                }

                return Json(new { success = false, message = "Invalid role specified" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error assigning user role: {ex.Message}");
                return Json(new { success = false, message = "Error assigning user role" });
            }
        }
    }
}