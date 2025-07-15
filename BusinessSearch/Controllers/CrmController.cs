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

        public CrmController(
            CrmService crmService,
    TeamService teamService,
    ILogger<CrmController> logger,
    IOrganizationFilterService orgFilter,
    ApplicationDbContext dbContext,
    IAntiforgery antiforgery)
        {
            _crmService = crmService;
            _teamService = teamService;
            _logger = logger;
            _orgFilter = orgFilter;
            _dbContext = dbContext;
            _antiforgery = antiforgery;
        }

        #region List Management

        [HttpGet]
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
                ViewBag.OrganizationId = orgId;

                var lists = await _crmService.GetAllLists();
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
                    // Find the user ID associated with the selected team member ID
                    var teamMember = teamMembers.FirstOrDefault(m => m.Id == assignedToFilter.Value);
                    if (teamMember != null)
                    {
                        // Look for any users associated with this team member
                        var userIds = await _dbContext.Users
                            .Where(u => u.TeamMemberId == teamMember.Id)
                            .Select(u => u.Id)
                            .ToListAsync();

                        if (userIds.Any())
                        {
                            // Filter lists where AssignedToId matches any of the user IDs
                            lists = lists.Where(l => l.AssignedToId != null && userIds.Contains(l.AssignedToId)).ToList();

                            // Log the filtering operation for debugging
                            _logger.LogInformation($"Filtering by team member ID {assignedToFilter.Value}, found {userIds.Count} user IDs, resulting in {lists.Count()} lists");
                        }
                        else
                        {
                            // No users found for this team member ID
                            _logger.LogWarning($"No users found for team member ID {assignedToFilter.Value}");
                            lists = new List<CrmList>(); // Return empty list if no matching users
                        }
                    }
                    else
                    {
                        // Team member not found
                        _logger.LogWarning($"Team member with ID {assignedToFilter.Value} not found");
                        lists = new List<CrmList>(); // Return empty list if team member not found
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
        public async Task<IActionResult> Edit(EditCrmEntryViewModel viewModel)
        {
            try
            {
                // Debug log to see what's coming in
                _logger.LogInformation($"Edit POST received for entry ID: {viewModel.Entry?.Id}");
                _logger.LogInformation($"Notes value: {viewModel.Entry?.Notes?.Substring(0, Math.Min(50, viewModel.Entry?.Notes?.Length ?? 0))}...");

                // Quick validation of entry
                if (viewModel.Entry == null)
                {
                    _logger.LogWarning("Entry is null in the viewModel");
                    ModelState.AddModelError("", "No entry data provided");
                    viewModel.AvailableLists = await _crmService.GetAllLists();
                    return View(viewModel);
                }

                // Check ModelState
                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    _logger.LogWarning($"ModelState invalid: {errors}");

                    viewModel.AvailableLists = await _crmService.GetAllLists();
                    return View(viewModel);
                }

                // Get existing entry
                var existingEntry = await _crmService.GetEntryById(viewModel.Entry.Id);
                if (existingEntry == null)
                {
                    _logger.LogWarning($"Entry with ID {viewModel.Entry.Id} not found");
                    return NotFound();
                }

                // Update all properties of existing entry
                existingEntry.BusinessName = viewModel.Entry.BusinessName;
                existingEntry.Phone = viewModel.Entry.Phone;
                existingEntry.Email = viewModel.Entry.Email;
                existingEntry.Website = viewModel.Entry.Website;
                existingEntry.Industry = viewModel.Entry.Industry;
                existingEntry.Disposition = viewModel.Entry.Disposition;
                existingEntry.GoogleRating = viewModel.Entry.GoogleRating;

                // Special debug log for Notes field
                _logger.LogInformation($"Original Notes value: {existingEntry.Notes}");
                _logger.LogInformation($"New Notes value from form: {viewModel.Entry.Notes}");

                // Update Notes
                existingEntry.Notes = viewModel.Entry.Notes;

                // Save entry with all changes
                await _crmService.UpdateEntry(existingEntry);

                // Update selected lists
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
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                ModelState.AddModelError("", $"Error updating entry: {ex.Message}");

                viewModel.AvailableLists = await _crmService.GetAllLists();
                return View(viewModel);
            }
        }

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
        public async Task<IActionResult> UpdateNotes([FromBody] UpdateNotesModel model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.Notes))
                {
                    return Json(new { success = false, message = "Notes cannot be empty" });
                }

                var entry = await _crmService.GetEntryById(model.Id);
                if (entry == null)
                {
                    return Json(new { success = false, message = "Entry not found" });
                }

                entry.Notes = model.Notes;
                await _crmService.UpdateEntry(entry);

                return Json(new { success = true, message = "Notes updated successfully" });
            }
            catch (UnauthorizedAccessException)
            {
                return Json(new { success = false, message = "Access denied" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating notes: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        public class UpdateNotesModel
        {
            public int Id { get; set; }
            public string Notes { get; set; }
        }

        #endregion

        #region Bulk Operations

        // In CrmController.cs
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveEntries([FromBody] BulkEntriesRequest request)
        {
            try
            {
                if (request.EntryIds == null || !request.EntryIds.Any())
                {
                    return Json(new { success = false, message = "No entries selected" });
                }

                // Get entries that already exist in the destination list
                var existingEntryIds = await _dbContext.CrmEntryLists
                    .Where(el => request.EntryIds.Contains(el.CrmEntryId) && el.CrmListId == request.DestinationListId)
                    .Select(el => el.CrmEntryId)
                    .ToListAsync();

                // Get entries that don't exist in the destination
                var entriesToMove = request.EntryIds.Except(existingEntryIds).ToArray();

                if (entriesToMove.Length == 0)
                {
                    return Json(new { success = false, message = "All selected entries already exist in the destination list" });
                }

                // Move only non-duplicate entries
                await _crmService.MoveEntries(entriesToMove, request.SourceListId, request.DestinationListId);

                // Return a success message with information about skipped entries
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

        #region AJAX Operations

        [HttpPost]
        [ValidateAntiForgeryToken]
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _crmService.DeleteEntry(id);
                return Json(new { success = true });
            }
            catch (UnauthorizedAccessException)
            {
                return Json(new { success = false, message = "Access denied" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting entry: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region List Management Actions

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateList([FromBody] object data)
        {
            try
            {
                _logger.LogInformation($"Received data: {data}");

                // Parse the data manually
                var jsonDocument = JsonDocument.Parse(data.ToString());
                var root = jsonDocument.RootElement;

                // Extract properties
                string name = root.GetProperty("name").GetString();
                string description = root.TryGetProperty("description", out var descProp) ?
                    descProp.GetString() : "";
                string industry = root.TryGetProperty("industry", out var indProp) ?
                    indProp.GetString() : "";

                if (string.IsNullOrWhiteSpace(name))
                {
                    return Json(new { success = false, message = "List name is required" });
                }

                // Get current user ID
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    // Fall back to getting the current user from DB context
                    var userManager = HttpContext.RequestServices.GetService<UserManager<ApplicationUser>>();
                    var currentUser = await userManager.GetUserAsync(User);
                    if (currentUser != null)
                    {
                        userId = currentUser.Id;
                    }
                    else
                    {
                        return Json(new { success = false, message = "Could not determine current user" });
                    }
                }

                // Get organization ID
                var orgId = await _orgFilter.GetCurrentOrganizationId();

                // Create the CrmList directly with DbContext to bypass any service layer issues
                var context = HttpContext.RequestServices.GetService<ApplicationDbContext>();

                // Create list with minimal required fields
                var newList = new CrmList
                {
                    Name = name,
                    Description = description,
                    Industry = industry,
                    CreatedDate = DateTime.UtcNow,
                    LastModifiedDate = DateTime.UtcNow,
                    CreatedById = userId,
                    OrganizationId = orgId,
                    // Explicitly set AssignedToId to null
                    AssignedToId = null
                };

                _logger.LogInformation($"Creating list with: Name={newList.Name}, " +
                    $"CreatedById={newList.CreatedById}, " +
                    $"OrganizationId={newList.OrganizationId}, " +
                    $"Industry={newList.Industry}");

                // Add to context and save directly
                context.CrmLists.Add(newList);
                await context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    listId = newList.Id,
                    listName = newList.Name
                });
            }
            catch (Exception ex)
            {
                // Log the inner exception if there is one
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception message: {ex.InnerException.Message}");
                    _logger.LogError($"Inner exception stack trace: {ex.InnerException.StackTrace}");
                }

                _logger.LogError($"Error creating list: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");

                return Json(new { success = false, message = $"Failed to create list: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ListEdit(int id)
        {
            try
            {
                var list = await _crmService.GetListById(id);
                if (list == null)
                {
                    return NotFound();
                }

                ViewBag.TeamMembers = await _teamService.GetAllTeamMembers();
                return View(list);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading list for edit: {ex.Message}");
                TempData["Error"] = "An error occurred while loading the list.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [Route("Crm/UpdateList")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateList(int id, string name, string description, string industry, string assignedToId)
        {
            try
            {
                // Log exactly what we're receiving
                _logger.LogInformation($"UpdateList method called with: id={id}, name={name}, assignedToId={assignedToId}");

                if (id <= 0)
                {
                    return Json(new { success = false, message = "Invalid ID" });
                }

                // Get existing list directly from DbContext
                var existingList = await _dbContext.CrmLists.FindAsync(id);
                if (existingList == null)
                {
                    _logger.LogWarning($"List with ID {id} not found");
                    return Json(new { success = false, message = "List not found" });
                }

                // Update fields
                existingList.Name = name;
                existingList.Description = description ?? "";
                existingList.Industry = industry ?? "";

                // Very explicit handling of assignedToId
                if (string.IsNullOrWhiteSpace(assignedToId))
                {
                    existingList.AssignedToId = null;
                    _logger.LogInformation("Setting AssignedToId to NULL");
                }
                else
                {
                    existingList.AssignedToId = assignedToId;
                    _logger.LogInformation($"Setting AssignedToId to: {assignedToId}");
                }

                existingList.LastModifiedDate = DateTime.UtcNow;

                try
                {
                    // Explicitly save just this entity
                    _dbContext.CrmLists.Update(existingList);
                    await _dbContext.SaveChangesAsync();

                    _logger.LogInformation($"List saved successfully with AssignedToId: {existingList.AssignedToId}");

                    return Json(new
                    {
                        success = true,
                        message = "List updated successfully",
                        listId = existingList.Id,
                        listName = existingList.Name,
                        assignedToId = existingList.AssignedToId ?? "null"
                    });
                }
                catch (DbUpdateException dbEx)
                {
                    _logger.LogError($"Database error: {dbEx.Message}");
                    if (dbEx.InnerException != null)
                    {
                        _logger.LogError($"Inner exception: {dbEx.InnerException.Message}");
                    }
                    return Json(new { success = false, message = $"Database error: {dbEx.Message}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception in UpdateList: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                }

                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MergeLists([FromBody] MergeListsViewModel viewModel)
        {
            try
            {
                await _crmService.MergeLists(
                    viewModel.SourceListId,
                    viewModel.CreateNewList ? null : viewModel.TargetListId,
                    viewModel.CreateNewList,
                    viewModel.NewListName);

                return Json(new { success = true });
            }
            catch (UnauthorizedAccessException)
            {
                return Json(new { success = false, message = "Access denied" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error merging lists: {ex.Message}");
                return Json(new { success = false, message = "Failed to merge lists" });
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

        public class SearchCriteria
        {
            public string? SearchTerm { get; set; }
            public string? IndustryFilter { get; set; }
            public int? AssignedToFilter { get; set; }
            public string? DispositionFilter { get; set; }
            public int PageSize { get; set; } = 25;
            public int Page { get; set; } = 1;
            public string? SortColumn { get; set; }
            public string? SortDirection { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableLists()
        {
            try
            {
                var lists = await _crmService.GetAllLists();

                // Create a simplified list without circular references
                var simplifiedLists = lists.Select(l => new
                {
                    id = l.Id,
                    name = l.Name,
                    description = l.Description,
                    industry = l.Industry,
                    entryCount = l.EntryCount,
                    assignedToId = l.AssignedToId
                }).ToList();

                return Json(new { success = true, lists = simplifiedLists });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching lists: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult CheckSchema()
        {
            try
            {
                var context = HttpContext.RequestServices.GetService(typeof(ApplicationDbContext)) as ApplicationDbContext;
                var properties = typeof(CrmEntry).GetProperties().Select(p => p.Name).ToList();
                var columnInfo = new List<object>();

                using (var command = context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'CrmEntries'";
                    context.Database.OpenConnection();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            columnInfo.Add(new
                            {
                                ColumnName = reader.GetString(0),
                                DataType = reader.GetString(1),
                                ExistsInModel = properties.Contains(reader.GetString(0))
                            });
                        }
                    }
                }

                // Find any model properties not in DB
                var missingColumns = properties.Where(p => !columnInfo.Any(c => ((dynamic)c).ColumnName == p)).ToList();

                return Json(new
                {
                    TableColumns = columnInfo,
                    ModelProperties = properties,
                    MissingColumns = missingColumns
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message, stack = ex.StackTrace });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateNotesOnly(int id, string notes)
        {
            try
            {
                _logger.LogInformation($"Updating notes for entry {id}");

                var entry = await _crmService.GetEntryById(id);
                if (entry == null)
                {
                    return NotFound();
                }

                // Update the notes
                entry.Notes = notes;
                await _crmService.UpdateEntry(entry);

                // Return success
                TempData["Success"] = "Notes updated successfully.";
                return RedirectToAction("BusinessView", new { id = id });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating notes: {ex.Message}");
                TempData["Error"] = "Failed to update notes.";
                return RedirectToAction("BusinessView", new { id = id });
            }
        }

        public IActionResult TestNotes(int id)
        {
            var entry = _crmService.GetEntryById(id).Result;
            return View(entry);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TestNotes(int id, string notes)
        {
            var entry = await _crmService.GetEntryById(id);
            if (entry != null)
            {
                entry.Notes = notes;
                await _crmService.UpdateEntry(entry);
                TempData["Success"] = "Notes updated successfully.";
            }
            return RedirectToAction("BusinessView", new { id = id });
        }

        [HttpGet]
        public async Task<IActionResult> GetListDetails(int id)
        {
            try
            {
                _logger.LogInformation($"GetListDetails called for list ID: {id}");

                var list = await _crmService.GetListById(id);
                if (list == null)
                {
                    _logger.LogWarning($"List with ID {id} not found");
                    return NotFound();
                }

                // Get all team members - use the same method as in Create to ensure consistency
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

        [HttpGet]
        public async Task<IActionResult> SimpleEdit(int id)
        {
            try
            {
                var list = await _crmService.GetListById(id);
                if (list == null)
                {
                    return NotFound();
                }

                return View(list);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in SimpleEdit: {ex.Message}");
                return Content($"Error: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("Crm/DirectUpdate/{id}")]
        public async Task<IActionResult> DirectUpdate(int id)
        {
            try
            {
                var list = await _crmService.GetListById(id);
                if (list == null)
                {
                    return NotFound();
                }

                // Get form values from query parameters or use existing values
                string name = Request.Query.ContainsKey("name") ? Request.Query["name"] : list.Name;

                // Update the list
                list.Name = name;
                // Update other fields as needed

                await _crmService.UpdateList(list);

                TempData["Success"] = "List updated successfully";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in DirectUpdate: {ex.Message}");
                TempData["Error"] = "Failed to update list: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [Route("Crm/SaveListDirect/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveListDirect(int id, IFormCollection form)
        {
            try
            {
                _logger.LogInformation($"SaveListDirect called with ID: {id}");

                // Get name (required field)
                string name = form["name"].ToString().Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    TempData["Error"] = "List name is required";
                    return RedirectToAction("ListEdit", new { id });
                }

                // Get optional fields
                string description = form["description"].ToString();
                string industry = form["industry"].ToString();
                string assignedToId = !string.IsNullOrEmpty(form["assignedToId"]) ? form["assignedToId"].ToString() : null;

                // Get existing list
                var existingList = await _dbContext.CrmLists.FindAsync(id);
                if (existingList == null)
                {
                    TempData["Error"] = "List not found";
                    return RedirectToAction("Index");
                }

                // Update fields
                existingList.Name = name;
                existingList.Description = description;
                existingList.Industry = industry;
                existingList.AssignedToId = assignedToId;
                existingList.LastModifiedDate = DateTime.UtcNow;

                // Get current user ID for LastModifiedById
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    existingList.LastModifiedById = userId;
                }

                // Save directly
                _dbContext.Entry(existingList).State = EntityState.Modified;
                await _dbContext.SaveChangesAsync();

                TempData["Success"] = "List updated successfully";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in SaveListDirect: {ex.Message}");
                TempData["Error"] = $"Error updating list: {ex.Message}";
                return RedirectToAction("ListEdit", new { id });
            }
        }

        [HttpGet]
        [Route("Crm/SaveListDirectGet/{id}")]
        public async Task<IActionResult> SaveListDirectGet(
            int id,
            string name,
            string description,
            string industry,
            string assignedToId,
            string __RequestVerificationToken)
        {
            try
            {
                _logger.LogInformation($"SaveListDirectGet called with ID: {id}");

                // Note: We're removing the antiforgery validation for GET requests
                // because they're not typically protected by antiforgery tokens

                if (string.IsNullOrWhiteSpace(name))
                {
                    TempData["Error"] = "List name is required";
                    return RedirectToAction("ListEdit", new { id });
                }

                var existingList = await _dbContext.CrmLists.FindAsync(id);
                if (existingList == null)
                {
                    TempData["Error"] = "List not found";
                    return RedirectToAction("Index");
                }

                // Update fields
                existingList.Name = name.Trim();
                existingList.Description = description ?? "";
                existingList.Industry = industry ?? "";
                existingList.AssignedToId = !string.IsNullOrEmpty(assignedToId) ? assignedToId : null;
                existingList.LastModifiedDate = DateTime.UtcNow;

                // Get current user ID
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    existingList.LastModifiedById = userId;
                }

                _dbContext.Entry(existingList).State = EntityState.Modified;
                await _dbContext.SaveChangesAsync();

                TempData["Success"] = "List updated successfully";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in SaveListDirectGet: {ex.Message}");
                TempData["Error"] = $"Error updating list: {ex.Message}";
                return RedirectToAction("ListEdit", new { id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuperSimpleSave(IFormCollection form)
        {
            try
            {
                // DEBUG: Log the raw form data
                _logger.LogInformation("SuperSimpleSave called");
                _logger.LogInformation($"Form data: {Request.Form.Count} items");
                foreach (var key in Request.Form.Keys)
                {
                    _logger.LogInformation($"Form field: {key} = {Request.Form[key]}");
                }

                // Get values directly
                string idStr = Request.Form["ListId"];
                _logger.LogInformation($"ListId value: '{idStr}'");

                if (string.IsNullOrEmpty(idStr))
                {
                    TempData["Error"] = "List ID is missing";
                    return RedirectToAction("Index");
                }

                if (!int.TryParse(idStr, out int id))
                {
                    TempData["Error"] = "Invalid list ID";
                    return RedirectToAction("Index");
                }

                // The rest of your code...
                string name = Request.Form["ListName"];
                string description = Request.Form["ListDescription"];
                string industry = Request.Form["ListIndustry"];
                string assignedToId = Request.Form["ListAssignedToId"];

                // ... Update code ...
                var list = await _dbContext.CrmLists.FindAsync(id);
                if (list == null)
                {
                    TempData["Error"] = "List not found";
                    return RedirectToAction("Index");
                }

                list.Name = name;
                list.Description = description;
                list.Industry = industry;
                list.AssignedToId = string.IsNullOrEmpty(assignedToId) ? null : assignedToId;
                list.LastModifiedDate = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                TempData["Success"] = "List updated successfully";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in SuperSimpleSave: {ex.Message}");
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [Route("Crm/DirectUpdate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DirectUpdate(int id, string assignedToId)
        {
            try
            {
                // Log exactly what we're receiving
                _logger.LogInformation($"DirectUpdate called with id={id}, assignedToId={assignedToId}");

                // Use raw SQL to update just the AssignedToId field
                string sql;
                if (string.IsNullOrEmpty(assignedToId))
                {
                    sql = "UPDATE CrmLists SET AssignedToId = NULL WHERE Id = @id";
                    await _dbContext.Database.ExecuteSqlRawAsync(sql, new SqlParameter("@id", id));
                }
                else
                {
                    sql = "UPDATE CrmLists SET AssignedToId = @assignedToId WHERE Id = @id";
                    await _dbContext.Database.ExecuteSqlRawAsync(sql,
                        new SqlParameter("@assignedToId", assignedToId),
                        new SqlParameter("@id", id));
                }

                return Json(new { success = true, message = "List updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in DirectUpdate: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                }
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route("Crm/AssignList")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignList(int id, string assignedToId)
        {
            try
            {
                _logger.LogInformation($"AssignList called with id={id}, assignedToId={assignedToId}");

                // Check if we should assign or unassign
                if (string.IsNullOrEmpty(assignedToId))
                {
                    // Set to NULL
                    await _dbContext.Database.ExecuteSqlRawAsync(
                        "UPDATE CrmLists SET AssignedToId = NULL, LastModifiedDate = GETDATE() WHERE Id = @id",
                        new SqlParameter("@id", id));
                }
                else
                {
                    // Update with the new ID
                    await _dbContext.Database.ExecuteSqlRawAsync(
                        "UPDATE CrmLists SET AssignedToId = @assignedToId, LastModifiedDate = GETDATE() WHERE Id = @id",
                        new SqlParameter("@assignedToId", assignedToId),
                        new SqlParameter("@id", id));
                }

                return Json(new { success = true, message = "Assignment updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in AssignList: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                }
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route("Crm/AssignUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignUser(int id, string assignedToId)
        {
            try
            {
                _logger.LogInformation($"AssignUser called with id={id}, assignedToId={assignedToId}");

                // Get the existing list
                var list = await _dbContext.CrmLists.FindAsync(id);
                if (list == null)
                {
                    return Json(new { success = false, message = "List not found" });
                }

                // Get the ApplicationUser ID associated with the TeamMember
                if (!string.IsNullOrEmpty(assignedToId))
                {
                    // If the input is a TeamMember ID (int), convert it to int and find the User
                    if (int.TryParse(assignedToId, out int teamMemberId))
                    {
                        var user = await _dbContext.Users
                            .FirstOrDefaultAsync(u => u.TeamMemberId == teamMemberId);

                        if (user != null)
                        {
                            assignedToId = user.Id; // Use the ApplicationUser.Id (string)
                            _logger.LogInformation($"Found User ID {assignedToId} for TeamMember ID {teamMemberId}");
                        }
                        else
                        {
                            // No user found for this team member
                            assignedToId = null;
                            _logger.LogWarning($"No user found for TeamMember ID {teamMemberId}");
                        }
                    }
                }

                // Update the list
                list.AssignedToId = assignedToId;
                list.LastModifiedDate = DateTime.UtcNow;

                // Get current user ID for LastModifiedById
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    list.LastModifiedById = userId;
                }

                await _dbContext.SaveChangesAsync();

                return Json(new { success = true, message = "Assignment updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in AssignUser: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                }
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route("Crm/DirectAssign")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DirectAssign(int id, string assignedToId)
        {
            try
            {
                _logger.LogInformation($"DirectAssign called with id={id}, assignedToId={assignedToId}");

                // Get the existing list
                var list = await _dbContext.CrmLists.FindAsync(id);
                if (list == null)
                {
                    return Json(new { success = false, message = "List not found" });
                }

                // Handle converting TeamMember ID to ApplicationUser ID
                if (!string.IsNullOrEmpty(assignedToId))
                {
                    // Check if assignedToId is a TeamMember ID (int)
                    if (int.TryParse(assignedToId, out int teamMemberId))
                    {
                        _logger.LogInformation($"Looking up ApplicationUser for TeamMember ID: {teamMemberId}");

                        // Find the ApplicationUser associated with this TeamMember
                        var user = await _dbContext.Users
                            .FirstOrDefaultAsync(u => u.TeamMemberId == teamMemberId);

                        if (user != null)
                        {
                            // Set the AssignedToId to the ApplicationUser's ID (string)
                            assignedToId = user.Id;
                            _logger.LogInformation($"Found ApplicationUser ID: {assignedToId}");
                        }
                        else
                        {
                            _logger.LogWarning($"No ApplicationUser found for TeamMember ID: {teamMemberId}");
                            assignedToId = null;
                        }
                    }
                }

                // Update the AssignedToId field
                if (string.IsNullOrEmpty(assignedToId))
                {
                    list.AssignedToId = null;
                    _logger.LogInformation("Setting AssignedToId to NULL");
                }
                else
                {
                    list.AssignedToId = assignedToId;
                    _logger.LogInformation($"Setting AssignedToId to: {assignedToId}");
                }

                list.LastModifiedDate = DateTime.UtcNow;

                // Get current user ID for LastModifiedById
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    list.LastModifiedById = userId;
                }

                // Save the changes
                await _dbContext.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Assignment updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in DirectAssign: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                }
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route("Crm/UpdateListDetails")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateListDetails(int id, string name, string description, string industry, string assignedToId)
        {
            try
            {
                _logger.LogInformation($"UpdateListDetails called with id={id}, name={name}, assignedToId={assignedToId}");

                if (string.IsNullOrWhiteSpace(name))
                {
                    return Json(new { success = false, message = "List name is required" });
                }

                // Get existing list
                var list = await _dbContext.CrmLists.FindAsync(id);
                if (list == null)
                {
                    return Json(new { success = false, message = "List not found" });
                }

                // Update all fields including AssignedToId
                list.Name = name;
                list.Description = description ?? "";
                list.Industry = industry ?? "";

                // Handle assignedToId properly
                if (string.IsNullOrEmpty(assignedToId))
                {
                    list.AssignedToId = null;
                    _logger.LogInformation("Setting AssignedToId to NULL");
                }
                else
                {
                    list.AssignedToId = assignedToId;
                    _logger.LogInformation($"Setting AssignedToId to: {assignedToId}");
                }

                list.LastModifiedDate = DateTime.UtcNow;

                // Get current user ID
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    list.LastModifiedById = userId;
                }

                await _dbContext.SaveChangesAsync();

                return Json(new { success = true, message = "List details updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in UpdateListDetails: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                }
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route("Crm/UpdateListFull")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateListFull(int id, string name, string description, string industry, string assignedToId)
        {
            try
            {
                _logger.LogInformation($"UpdateListFull called with id={id}, name={name}, assignedToId={assignedToId}");

                if (string.IsNullOrWhiteSpace(name))
                {
                    return Json(new { success = false, message = "List name is required" });
                }

                // Get existing list
                var list = await _dbContext.CrmLists.FindAsync(id);
                if (list == null)
                {
                    return Json(new { success = false, message = "List not found" });
                }

                // Update the basic fields
                list.Name = name;
                list.Description = description ?? "";
                list.Industry = industry ?? "";
                list.LastModifiedDate = DateTime.UtcNow;

                // Handle assignedToId conversion if needed
                if (!string.IsNullOrEmpty(assignedToId))
                {
                    // Check if this is a TeamMember ID (integer)
                    if (int.TryParse(assignedToId, out int teamMemberId))
                    {
                        _logger.LogInformation($"Looking up ApplicationUser for TeamMember ID: {teamMemberId}");

                        // Find the corresponding ApplicationUser
                        var user = await _dbContext.Users
                            .FirstOrDefaultAsync(u => u.TeamMemberId == teamMemberId);

                        if (user != null)
                        {
                            // Use the ApplicationUser's ID
                            list.AssignedToId = user.Id;
                            _logger.LogInformation($"Found ApplicationUser ID: {user.Id}");
                        }
                        else
                        {
                            _logger.LogWarning($"No ApplicationUser found for TeamMember ID: {teamMemberId}");
                            list.AssignedToId = null;
                        }
                    }
                    else
                    {
                        // It's already in the correct format (string)
                        list.AssignedToId = assignedToId;
                    }
                }
                else
                {
                    // Clear the assignment
                    list.AssignedToId = null;
                    _logger.LogInformation("Setting AssignedToId to NULL");
                }

                // Update last modified user ID
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    list.LastModifiedById = userId;
                }

                // Save all changes
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
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                    _logger.LogError($"Inner exception stack trace: {ex.InnerException.StackTrace}");
                }
                return Json(new { success = false, message = $"Error updating list: {ex.Message}" });
            }
        }

        #endregion
    }
}