using BusinessSearch.Authorization;
using BusinessSearch.Models;
using BusinessSearch.Models.ViewModels;
using BusinessSearch.Services;
using BusinessSearch.DTOs;
using BusinessSearch.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using BusinessSearch.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Data.SqlClient;
using BusinessSearch.Models.Organization;
using SendGrid.Helpers.Mail;

namespace BusinessSearch.Controllers
{
    [Authorize]
    [RequireOrganization]
    public class CrmController : Controller
    {
        private readonly CrmService _crmService;
        private readonly TeamService _teamService;
        private readonly ILogger<CrmController> _logger;
        private readonly IOrganizationFilterService _orgFilter;
        private readonly ApplicationDbContext _dbContext;
        private readonly IAntiforgery _antiforgery;
        private readonly UserManager<ApplicationUser> _userManager;

        public CrmController(
            CrmService crmService,
            TeamService teamService,
            ILogger<CrmController> logger,
            IOrganizationFilterService orgFilter,
            ApplicationDbContext dbContext,
            IAntiforgery antiforgery,
            UserManager<ApplicationUser> userManager)
        {
            _crmService = crmService;
            _teamService = teamService;
            _logger = logger;
            _orgFilter = orgFilter;
            _dbContext = dbContext;
            _antiforgery = antiforgery;
            _userManager = userManager;
        }

        #region List Management

        [HttpGet]
        [RoleBasedAuthorization(OrganizationRole.Admin, OrganizationRole.Member, OrganizationRole.Caller)]
        public async Task<IActionResult> Index(
            int pageSize = 25,
            int page = 1,
            string? searchTerm = null,
            string? industryFilter = null,
            int? assignedToFilter = null,
            string? sortColumn = null,
            string? sortDirection = null)
        {
            try
            {
                var orgId = await _orgFilter.GetCurrentOrganizationId();
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var currentUser = await _userManager.GetUserAsync(User);
                ViewBag.OrganizationId = orgId;
                ViewBag.CurrentUser = currentUser;

                // Get lists based on user role
                var lists = await _crmService.GetAllLists(currentUserId);
                var teamMembers = await _teamService.GetAllTeamMembers();

                // Apply filters
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    lists = lists.Where(l =>
                        l.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        (l.Description != null && l.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                if (!string.IsNullOrEmpty(industryFilter))
                {
                    lists = lists.Where(l => l.Industry == industryFilter).ToList();
                }

                if (assignedToFilter.HasValue)
                {
                    var teamMember = teamMembers.FirstOrDefault(m => m.Id == assignedToFilter.Value);
                    if (teamMember != null)
                    {
                        var userIds = await _dbContext.Users
                            .Where(u => u.TeamMemberId == teamMember.Id)
                            .Select(u => u.Id)
                            .ToListAsync();

                        if (userIds.Any())
                        {
                            lists = lists.Where(l => l.AssignedToId != null && userIds.Contains(l.AssignedToId)).ToList();
                            _logger.LogInformation($"Filtering by team member ID {assignedToFilter.Value}, found {userIds.Count} user IDs, resulting in {lists.Count()} lists");
                        }
                        else
                        {
                            _logger.LogWarning($"No users found for team member ID {assignedToFilter.Value}");
                            lists = new List<CrmList>();
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Team member with ID {assignedToFilter.Value} not found");
                        lists = new List<CrmList>();
                    }
                }

                // Apply sorting
                if (!string.IsNullOrEmpty(sortColumn))
                {
                    lists = SortLists(lists, sortColumn, sortDirection).ToList();
                }

                // Calculate pagination
                var totalItems = lists.Count();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
                page = Math.Max(1, Math.Min(page, totalPages));

                var pagedLists = lists
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return View("~/Views/Crm/Index.cshtml", new CrmListIndexViewModel
                {
                    Lists = pagedLists,
                    TeamMembers = teamMembers,
                    CurrentPage = page,
                    TotalPages = totalPages,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    SearchTerm = searchTerm,
                    IndustryFilter = industryFilter,
                    AssignedToFilter = assignedToFilter
                });
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in CRM Index: {ex.Message}");
                TempData["Error"] = "An error occurred while loading the lists.";
                return View("~/Views/Crm/Index.cshtml", new CrmListIndexViewModel());
            }
        }

        [RoleBasedAuthorization(true, OrganizationRole.Admin, OrganizationRole.Member, OrganizationRole.Caller)]
        public async Task<IActionResult> ListView(
            int id,
            int pageSize = 25,
            int page = 1,
            string? searchTerm = null,
            string? dispositionFilter = null,
            string? sortColumn = null,
            string? sortDirection = null)
        {
            try
            {
                var list = await _crmService.GetListById(id);
                if (list == null)
                {
                    return NotFound();
                }

                var entries = list.CrmEntryLists
                    .Select(el => el.CrmEntry)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    entries = entries.Where(e =>
                        e.BusinessName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        (e.Industry != null && e.Industry.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                        (e.Email != null && e.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                        (e.Phone != null && e.Phone.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    );
                }

                if (!string.IsNullOrEmpty(dispositionFilter))
                {
                    entries = entries.Where(e => e.Disposition == dispositionFilter);
                }

                // Apply sorting
                if (!string.IsNullOrEmpty(sortColumn))
                {
                    entries = SortEntries(entries, sortColumn, sortDirection);
                }

                // Calculate pagination
                var totalItems = entries.Count();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
                page = Math.Max(1, Math.Min(page, totalPages));

                var pagedEntries = entries
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var teamMembers = await _teamService.GetAllTeamMembers();
                var availableLists = await _crmService.GetAllLists();
                var currentUser = await _userManager.GetUserAsync(User);

                var viewModel = new CrmListViewModel
                {
                    List = list,
                    Entries = pagedEntries,
                    TeamMembers = teamMembers,
                    AvailableLists = availableLists.Where(l => l.Id != id).ToList(),
                    CurrentPage = page,
                    TotalPages = totalPages,
                    PageSize = pageSize,
                    SearchTerm = searchTerm,
                    DispositionFilter = dispositionFilter
                };

                ViewBag.CurrentUser = currentUser;
                return View(viewModel);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in ListView: {ex.Message}");
                TempData["Error"] = "An error occurred while loading the list entries.";
                return RedirectToAction(nameof(Index));
            }
        }

        #endregion

        #region Entry Management

        [RoleBasedAuthorization(OrganizationRole.Admin, OrganizationRole.Member)]
        public async Task<IActionResult> Create(int? listId = null)
        {
            try
            {
                var lists = await _crmService.GetAllLists();
                var listDtos = new List<CrmListDto>();

                foreach (var list in lists)
                {
                    listDtos.Add(DtoExtensions.ToDto(list));
                }

                var viewModel = new CreateCrmEntryDto
                {
                    Entry = new CrmEntryDto
                    {
                        DateAdded = DateTime.UtcNow,
                        Disposition = "New"
                    },
                    SelectedListId = listId,
                    AvailableLists = listDtos
                };

                return View(viewModel);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Create GET: {ex.Message}");
                TempData["Error"] = "An error occurred while loading the create form.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleBasedAuthorization(OrganizationRole.Admin, OrganizationRole.Member)]
        public async Task<IActionResult> Create([FromBody] CreateCrmEntryDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Validation failed",
                        errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }

                if (model.Entry == null)
                {
                    return Json(new { success = false, message = "Entry data is required" });
                }

                if (!model.SelectedListId.HasValue)
                {
                    return Json(new { success = false, message = "Please select a list" });
                }

                var entry = DtoExtensions.ToModel(model.Entry);
                var createdEntry = await _crmService.AddEntry(entry, model.SelectedListId);

                return Json(new
                {
                    success = true,
                    entryId = createdEntry.Id,
                    message = "Entry created successfully"
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Json(new { success = false, message = "Access denied" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Create: {ex.Message}");
                return Json(new { success = false, message = $"Error creating entry: {ex.Message}" });
            }
        }

        [RoleBasedAuthorization(true, OrganizationRole.Admin, OrganizationRole.Member, OrganizationRole.Caller)]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var entry = await _crmService.GetEntryById(id);
                if (entry == null)
                {
                    return NotFound();
                }

                var lists = await _crmService.GetAllLists();
                var viewModel = new EditCrmEntryViewModel
                {
                    Entry = entry,
                    AvailableLists = lists,
                    CurrentListIds = entry.CrmEntryLists.Select(el => el.CrmListId).ToList()
                };

                return View(viewModel);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Edit GET: {ex.Message}");
                TempData["Error"] = "An error occurred while loading the entry.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleBasedAuthorization(true, OrganizationRole.Admin, OrganizationRole.Member, OrganizationRole.Caller)]
        public async Task<IActionResult> Edit(EditCrmEntryViewModel viewModel)
        {
            try
            {
                _logger.LogInformation($"Edit POST received for entry ID: {viewModel.Entry?.Id}");

                if (viewModel.Entry == null)
                {
                    _logger.LogWarning("Entry is null in the viewModel");
                    ModelState.AddModelError("", "No entry data provided");
                    viewModel.AvailableLists = await _crmService.GetAllLists();
                    return View(viewModel);
                }

                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    _logger.LogWarning($"ModelState invalid: {errors}");

                    viewModel.AvailableLists = await _crmService.GetAllLists();
                    return View(viewModel);
                }

                var existingEntry = await _crmService.GetEntryById(viewModel.Entry.Id);
                if (existingEntry == null)
                {
                    _logger.LogWarning($"Entry with ID {viewModel.Entry.Id} not found");
                    return NotFound();
                }

                // Update all properties
                existingEntry.BusinessName = viewModel.Entry.BusinessName;
                existingEntry.Phone = viewModel.Entry.Phone;
                existingEntry.Email = viewModel.Entry.Email;
                existingEntry.Website = viewModel.Entry.Website;
                existingEntry.Industry = viewModel.Entry.Industry;
                existingEntry.Disposition = viewModel.Entry.Disposition;
                existingEntry.GoogleRating = viewModel.Entry.GoogleRating;
                existingEntry.Notes = viewModel.Entry.Notes;

                await _crmService.UpdateEntry(existingEntry);

                if (viewModel.SelectedListIds != null && viewModel.SelectedListIds.Any())
                {
                    await _crmService.UpdateEntryLists(viewModel.Entry.Id, viewModel.SelectedListIds);
                    TempData["Success"] = "Entry updated successfully.";
                    return RedirectToAction("ListView", new { id = viewModel.SelectedListIds.First() });
                }
                else
                {
                    TempData["Success"] = "Entry updated successfully, but no lists selected.";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating entry: {ex.Message}");
                ModelState.AddModelError("", $"Error updating entry: {ex.Message}");

                viewModel.AvailableLists = await _crmService.GetAllLists();
                return View(viewModel);
            }
        }

        [RoleBasedAuthorization(true, OrganizationRole.Admin, OrganizationRole.Member, OrganizationRole.Caller)]
        public async Task<IActionResult> BusinessView(int id)
        {
            try
            {
                var entry = await _crmService.GetEntryById(id);
                if (entry == null)
                {
                    return NotFound();
                }

                var viewModel = new BusinessViewModel
                {
                    Id = entry.Id,
                    Name = entry.BusinessName,
                    PhoneNumber = entry.Phone,
                    Email = entry.Email,
                    Website = entry.Website,
                    FullAddress = entry.FullAddress,
                    Rating = entry.GoogleRating ?? 0,
                    ReviewCount = entry.ReviewCount ?? 0,
                    Type = entry.Industry,
                    BusinessStatus = entry.BusinessStatus,
                    OpeningStatus = entry.OpeningStatus,
                    PhotoUrl = entry.PhotoUrl,
                    Facebook = entry.Facebook,
                    Instagram = entry.Instagram,
                    YelpUrl = entry.YelpUrl,
                    Notes = entry.Notes,
                    Disposition = entry.Disposition,
                    DateAdded = entry.DateAdded,
                    Lists = entry.CrmEntryLists.Select(el => el.CrmList).ToList()
                };

                var currentUser = await _userManager.GetUserAsync(User);
                ViewBag.CurrentUser = currentUser;

                return View(viewModel);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in BusinessView: {ex.Message}");
                TempData["Error"] = "An error occurred while loading the business details.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleBasedAuthorization(true, OrganizationRole.Admin, OrganizationRole.Member, OrganizationRole.Caller)]
        public async Task<IActionResult> UpdateDisposition([FromBody] DispositionUpdateModel model)
        {
            try
            {
                var entry = await _crmService.GetEntryById(model.Id);
                if (entry == null)
                {
                    return Json(new { success = false, message = "Entry not found" });
                }

                entry.Disposition = model.Disposition;
                await _crmService.UpdateEntry(entry);
                return Json(new { success = true });
            }
            catch (UnauthorizedAccessException)
            {
                return Json(new { success = false, message = "Access denied" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating disposition: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region List Management Actions (Admin/Member Only)

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleBasedAuthorization(OrganizationRole.Admin, OrganizationRole.Member)]
        public async Task<IActionResult> CreateList([FromBody] object data)
        {
            try
            {
                _logger.LogInformation($"Received data: {data}");

                var jsonDocument = JsonDocument.Parse(data.ToString());
                var root = jsonDocument.RootElement;

                string name = root.GetProperty("name").GetString();
                string description = root.TryGetProperty("description", out var descProp) ?
                    descProp.GetString() : "";
                string industry = root.TryGetProperty("industry", out var indProp) ?
                    indProp.GetString() : "";
                string assignedToId = root.TryGetProperty("assignedToId", out var assignedProp) ?
                    assignedProp.GetString() : null;

                if (string.IsNullOrWhiteSpace(name))
                {
                    return Json(new { success = false, message = "List name is required" });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    if (currentUser != null)
                    {
                        userId = currentUser.Id;
                    }
                    else
                    {
                        return Json(new { success = false, message = "Could not determine current user" });
                    }
                }

                var orgId = await _orgFilter.GetCurrentOrganizationId();

                // Handle assignedToId conversion if it's a TeamMember ID
                string finalAssignedToId = null;
                if (!string.IsNullOrEmpty(assignedToId))
                {
                    if (int.TryParse(assignedToId, out int teamMemberId))
                    {
                        var user = await _dbContext.Users
                            .FirstOrDefaultAsync(u => u.TeamMemberId == teamMemberId);
                        if (user != null)
                        {
                            finalAssignedToId = user.Id;
                            _logger.LogInformation($"Converted TeamMember ID {teamMemberId} to User ID {user.Id}");
                        }
                        else
                        {
                            _logger.LogWarning($"No user found for TeamMember ID {teamMemberId}");
                        }
                    }
                    else
                    {
                        // It's already a User ID
                        finalAssignedToId = assignedToId;
                    }
                }

                var newList = new CrmList
                {
                    Name = name,
                    Description = description,
                    Industry = industry,
                    CreatedDate = DateTime.UtcNow,
                    LastModifiedDate = DateTime.UtcNow,
                    CreatedById = userId,
                    OrganizationId = orgId,
                    AssignedToId = finalAssignedToId
                };

                _logger.LogInformation($"Creating list with AssignedToId: {finalAssignedToId}");

                _dbContext.CrmLists.Add(newList);
                await _dbContext.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    listId = newList.Id,
                    listName = newList.Name,
                    assignedToId = newList.AssignedToId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating list: {ex.Message}");
                return Json(new { success = false, message = $"Failed to create list: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleBasedAuthorization(OrganizationRole.Admin, OrganizationRole.Member)]
        public async Task<IActionResult> DeleteList(int id)
        {
            try
            {
                await _crmService.DeleteList(id);
                return Json(new { success = true });
            }
            catch (UnauthorizedAccessException)
            {
                return Json(new { success = false, message = "Access denied" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting list: {ex.Message}");
                return Json(new { success = false, message = "Failed to delete list" });
            }
        }

        #endregion

        #region Bulk Operations (Admin/Member Only)

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleBasedAuthorization(OrganizationRole.Admin, OrganizationRole.Member)]
        public async Task<IActionResult> MoveEntries([FromBody] BulkEntriesRequest request)
        {
            try
            {
                if (request.EntryIds == null || !request.EntryIds.Any())
                {
                    return Json(new { success = false, message = "No entries selected" });
                }

                var existingEntryIds = await _dbContext.CrmEntryLists
                    .Where(el => request.EntryIds.Contains(el.CrmEntryId) && el.CrmListId == request.DestinationListId)
                    .Select(el => el.CrmEntryId)
                    .ToListAsync();

                var entriesToMove = request.EntryIds.Except(existingEntryIds).ToArray();

                if (entriesToMove.Length == 0)
                {
                    return Json(new { success = false, message = "All selected entries already exist in the destination list" });
                }

                await _crmService.MoveEntries(entriesToMove, request.SourceListId, request.DestinationListId);

                if (existingEntryIds.Any())
                {
                    return Json(new
                    {
                        success = true,
                        message = $"Moved {entriesToMove.Length} entries. Skipped {existingEntryIds.Count} entries that already existed in the destination list."
                    });
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error moving entries: {ex.Message}");
                return Json(new { success = false, message = $"Failed to move entries: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleBasedAuthorization(OrganizationRole.Admin, OrganizationRole.Member)]
        public async Task<IActionResult> CopyEntries([FromBody] BulkEntriesRequest request)
        {
            try
            {
                if (request.EntryIds == null || !request.EntryIds.Any())
                {
                    return Json(new { success = false, message = "No entries selected" });
                }

                await _crmService.CopyEntries(request.EntryIds, request.SourceListId, request.DestinationListId);
                return Json(new { success = true });
            }
            catch (UnauthorizedAccessException)
            {
                return Json(new { success = false, message = "Access denied" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error copying entries: {ex.Message}");
                return Json(new { success = false, message = "Failed to copy entries" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleBasedAuthorization(OrganizationRole.Admin, OrganizationRole.Member)]
        public async Task<IActionResult> DeleteEntries([FromBody] BulkDeleteRequest request)
        {
            try
            {
                if (request.EntryIds == null || !request.EntryIds.Any())
                {
                    return Json(new { success = false, message = "No entries selected" });
                }

                await _crmService.DeleteEntriesFromList(request.EntryIds, request.ListId);
                return Json(new { success = true });
            }
            catch (UnauthorizedAccessException)
            {
                return Json(new { success = false, message = "Access denied" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting entries: {ex.Message}");
                return Json(new { success = false, message = "Failed to delete entries" });
            }
        }

        #endregion

        #region Update Methods (Admin/Member Only for Assignment)

        [HttpPost]
        [Route("Crm/UpdateListFull")]
        [ValidateAntiForgeryToken]
        [RoleBasedAuthorization(OrganizationRole.Admin, OrganizationRole.Member)]
        public async Task<IActionResult> UpdateListFull(int id, string name, string description, string industry, string assignedToId)
        {
            try
            {
                _logger.LogInformation($"UpdateListFull called with id={id}, name={name}, assignedToId={assignedToId}");

                if (string.IsNullOrWhiteSpace(name))
                {
                    return Json(new { success = false, message = "List name is required" });
                }

                var list = await _dbContext.CrmLists.FindAsync(id);
                if (list == null)
                {
                    return Json(new { success = false, message = "List not found" });
                }

                list.Name = name;
                list.Description = description ?? "";
                list.Industry = industry ?? "";
                list.LastModifiedDate = DateTime.UtcNow;

                if (!string.IsNullOrEmpty(assignedToId))
                {
                    if (int.TryParse(assignedToId, out int teamMemberId))
                    {
                        var user = await _dbContext.Users
                            .FirstOrDefaultAsync(u => u.TeamMemberId == teamMemberId);

                        if (user != null)
                        {
                            list.AssignedToId = user.Id;
                        }
                        else
                        {
                            list.AssignedToId = null;
                        }
                    }
                    else
                    {
                        list.AssignedToId = assignedToId;
                    }
                }
                else
                {
                    list.AssignedToId = null;
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    list.LastModifiedById = userId;
                }

                await _dbContext.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "List updated successfully",
                    list = new
                    {
                        id = list.Id,
                        name = list.Name,
                        description = list.Description,
                        industry = list.Industry,
                        assignedToId = list.AssignedToId
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in UpdateListFull: {ex.Message}");
                return Json(new { success = false, message = $"Error updating list: {ex.Message}" });
            }
        }

        #endregion

        #region API Methods for List Access

        [HttpGet]
        [RoleBasedAuthorization(OrganizationRole.Admin, OrganizationRole.Member, OrganizationRole.Caller)]
        public async Task<IActionResult> GetAvailableLists()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                // Get lists based on user role
                var lists = await _dbContext.CrmLists
                    .Where(l => l.OrganizationId == currentUser.OrganizationId)
                    .Where(l => currentUser.OrganizationRole == OrganizationRole.Admin ||
                               currentUser.OrganizationRole == OrganizationRole.Member ||
                               (currentUser.OrganizationRole == OrganizationRole.Caller && l.AssignedToId == currentUser.Id))
                    .Select(l => new
                    {
                        id = l.Id,
                        name = l.Name
                    })
                    .OrderBy(l => l.name)
                    .ToListAsync();

                return Json(new { success = true, lists = lists });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available lists");
                return Json(new { success = false, message = "An error occurred while loading lists" });
            }
        }

        [HttpGet]
        [RoleBasedAuthorization(OrganizationRole.Admin, OrganizationRole.Member, OrganizationRole.Caller)]
        public async Task<IActionResult> GetAvailableListsForAdd()
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var lists = await _crmService.GetAllLists(currentUserId);

                var simplifiedLists = lists.Select(l => new
                {
                    id = l.Id,
                    name = l.Name,
                    description = l.Description,
                    industry = l.Industry,
                    entryCount = l.EntryCount
                }).ToList();

                return Json(new { success = true, lists = simplifiedLists });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching available lists: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [RoleBasedAuthorization(OrganizationRole.Admin, OrganizationRole.Member)]
        public async Task<IActionResult> GetListDetails(int id)
        {
            try
            {
                _logger.LogInformation($"GetListDetails called for list ID: {id}");

                var list = await _crmService.GetListById(id);
                if (list == null)
                {
                    _logger.LogWarning($"List with ID {id} not found");
                    return Json(new { success = false, message = "List not found" });
                }

                // Get all team members - use the same method as in other actions
                var teamMembers = await _teamService.GetAllTeamMembers();

                // Create a simplified list without circular references
                var simplifiedList = new
                {
                    id = list.Id,
                    name = list.Name,
                    description = list.Description,
                    industry = list.Industry,
                    assignedToId = list.AssignedToId,
                    createdDate = list.CreatedDate,
                    entryCount = list.CrmEntryLists?.Count ?? 0
                };

                var simplifiedTeamMembers = teamMembers.Select(m => new
                {
                    id = m.Id,
                    name = m.Name
                }).ToList();

                // Return the list as JSON
                return Json(new
                {
                    success = true,
                    list = simplifiedList,
                    teamMembers = simplifiedTeamMembers
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting list details: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Helper Methods

        private IEnumerable<CrmList> SortLists(IEnumerable<CrmList> lists, string column, string? direction)
        {
            var isAscending = direction?.ToLower() != "desc";

            return column.ToLower() switch
            {
                "name" => isAscending ? lists.OrderBy(l => l.Name) : lists.OrderByDescending(l => l.Name),
                "industry" => isAscending ? lists.OrderBy(l => l.Industry) : lists.OrderByDescending(l => l.Industry),
                "assignedto" => isAscending
                    ? lists.OrderBy(l => l.AssignedTo == null ? "" : l.AssignedTo.Name)
                    : lists.OrderByDescending(l => l.AssignedTo == null ? "" : l.AssignedTo.Name),
                "entrycount" => isAscending ? lists.OrderBy(l => l.EntryCount) : lists.OrderByDescending(l => l.EntryCount),
                "createddate" => isAscending ? lists.OrderBy(l => l.CreatedDate) : lists.OrderByDescending(l => l.CreatedDate),
                _ => lists.OrderBy(l => l.Name)
            };
        }

        private IQueryable<CrmEntry> SortEntries(IQueryable<CrmEntry> entries, string column, string? direction)
        {
            var isAscending = direction?.ToLower() != "desc";

            return column.ToLower() switch
            {
                "businessname" => isAscending ? entries.OrderBy(e => e.BusinessName) : entries.OrderByDescending(e => e.BusinessName),
                "industry" => isAscending ? entries.OrderBy(e => e.Industry) : entries.OrderByDescending(e => e.Industry),
                "email" => isAscending ? entries.OrderBy(e => e.Email) : entries.OrderByDescending(e => e.Email),
                "phone" => isAscending ? entries.OrderBy(e => e.Phone) : entries.OrderByDescending(e => e.Phone),
                "website" => isAscending ? entries.OrderBy(e => e.Website) : entries.OrderByDescending(e => e.Website),
                "disposition" => isAscending ? entries.OrderBy(e => e.Disposition) : entries.OrderByDescending(e => e.Disposition),
                "dateadded" => isAscending ? entries.OrderBy(e => e.DateAdded) : entries.OrderByDescending(e => e.DateAdded),
                _ => entries.OrderBy(e => e.BusinessName)
            };
        }

        #endregion

        #region Models

        public class BulkEntriesRequest
        {
            public int[] EntryIds { get; set; }
            public int SourceListId { get; set; }
            public int DestinationListId { get; set; }
        }

        public class BulkDeleteRequest
        {
            public int[] EntryIds { get; set; }
            public int ListId { get; set; }
        }

        public class DispositionUpdateModel
        {
            public int Id { get; set; }
            public string Disposition { get; set; }
        }

        #endregion
    }
}