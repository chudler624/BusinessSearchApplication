using BusinessSearch.Models;
using BusinessSearch.Models.ViewModels;
using BusinessSearch.Services;
using BusinessSearch.DTOs;           
using BusinessSearch.Extensions;     
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace BusinessSearch.Controllers
{
    public class CrmController : Controller
    {
        private readonly CrmService _crmService;
        private readonly TeamService _teamService;
        private readonly ILogger<CrmController> _logger;

        public CrmController(
            CrmService crmService,
            TeamService teamService,
            ILogger<CrmController> logger)
        {
            _crmService = crmService;
            _teamService = teamService;
            _logger = logger;
        }

        #region List Management

        // Lists Overview
        // CrmController.cs - Update the Index action
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
                    lists = lists.Where(l => l.AssignedToId == assignedToFilter).ToList();
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
            catch (Exception ex)
            {
                _logger.LogError($"Error in CRM Index: {ex.Message}");
                TempData["Error"] = "An error occurred while loading the lists.";
                return View("~/Views/Crm/Index.cshtml", new CrmListIndexViewModel());
            }
        }

        // List View with Entries
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
                var viewModel = new CreateCrmEntryDto
                {
                    Entry = new CrmEntryDto
                    {
                        DateAdded = DateTime.UtcNow,
                        Disposition = "New"
                    },
                    SelectedListId = listId,  
                    AvailableLists = lists.Select(l => l.ToDto()).ToList()
                };
                return View(viewModel);
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
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    _logger.LogWarning($"Invalid model state: {string.Join(", ", errors)}");

                    return Json(new
                    {
                        success = false,
                        message = "Validation failed",
                        errors = errors
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

                var entry = model.Entry.ToModel();
                var createdEntry = await _crmService.AddEntry(entry, model.SelectedListId);

                return Json(new
                {
                    success = true,
                    entryId = createdEntry.Id,
                    message = "Entry created successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Create: {ex.Message}");
                return Json(new { success = false, message = $"Error creating entry: {ex.Message}" });
            }
        }

        public class CreateEntryRequestModel
        {
            public CrmEntry Entry { get; set; }
            public int? SelectedListId { get; set; }
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
                if (ModelState.IsValid)
                {
                    await _crmService.UpdateEntry(viewModel.Entry);
                    await _crmService.UpdateEntryLists(viewModel.Entry.Id, viewModel.SelectedListIds);
                    TempData["Success"] = "Entry updated successfully.";
                    return RedirectToAction("ListView", new { id = viewModel.SelectedListIds.First() });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating entry: {ex.Message}");
                ModelState.AddModelError("", "An error occurred while updating the entry.");
            }

            viewModel.AvailableLists = await _crmService.GetAllLists();
            return View(viewModel);
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
            catch (Exception ex)
            {
                _logger.LogError($"Error in BusinessView: {ex.Message}");
                TempData["Error"] = "An error occurred while loading the business details.";
                return RedirectToAction(nameof(Index));
            }
        }

        #endregion

        #region Bulk Operations

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

                await _crmService.MoveEntries(request.EntryIds, request.SourceListId, request.DestinationListId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error moving entries: {ex.Message}");
                return Json(new { success = false, message = "Failed to move entries" });
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
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting entry: {ex.Message}");
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
            "assignedto" => isAscending ? lists.OrderBy(l => l.AssignedTo.Name) : lists.OrderByDescending(l => l.AssignedTo.Name),
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

        #region List Management Actions

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateList([FromBody] CrmList list)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(list.Name))
                {
                    return Json(new { success = false, message = "List name is required" });
                }

                list.CreatedDate = DateTime.UtcNow;
                var createdList = await _crmService.CreateList(list);
                return Json(new { success = true, listId = createdList.Id, listName = createdList.Name });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating list: {ex.Message}");
                return Json(new { success = false, message = "Failed to create list" });
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

                // Get team members for the dropdown
                ViewBag.TeamMembers = await _teamService.GetAllTeamMembers();

                return View(list);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading list for edit: {ex.Message}");
                TempData["Error"] = "An error occurred while loading the list.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateList([FromBody] CrmList list)  // Add [FromBody] attribute
        {
            try
            {
                if (string.IsNullOrWhiteSpace(list.Name))
                {
                    return Json(new { success = false, message = "List name is required" });
                }

                var existingList = await _crmService.GetListById(list.Id);
                if (existingList == null)
                {
                    return Json(new { success = false, message = "List not found" });
                }

                // Preserve creation date and update last modified date
                list.CreatedDate = existingList.CreatedDate;
                list.LastModifiedDate = DateTime.UtcNow;

                await _crmService.UpdateList(list);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating list: {ex.Message}");
                return Json(new { success = false, message = "Failed to update list" });
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
            catch (Exception ex)
            {
                _logger.LogError($"Error merging lists: {ex.Message}");
                return Json(new { success = false, message = "Failed to merge lists" });
            }
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

        #endregion

        #region API Methods

        [HttpGet]
        public async Task<IActionResult> GetAvailableLists()
        {
            try
            {
                var lists = await _crmService.GetAllLists();
                // Transform the data to avoid serialization issues
                var listData = lists.Select(l => new {
                    id = l.Id,
                    name = l.Name,
                    description = l.Description,
                    industry = l.Industry
                });
                return Json(new { success = true, lists = listData });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting available lists: {ex.Message}");
                return Json(new { success = false, message = "Failed to get lists" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetListDetails(int id)
        {
            try
            {
                var list = await _crmService.GetListById(id);
                if (list == null)
                {
                    return Json(new { success = false, message = "List not found" });
                }

                // Transform the data to avoid serialization issues
                var listData = new
                {
                    id = list.Id,
                    name = list.Name,
                    description = list.Description,
                    industry = list.Industry,
                    assignedToId = list.AssignedToId,
                    assignedToName = list.AssignedTo?.Name,
                    entryCount = list.EntryCount
                };

                return Json(new { success = true, list = listData });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting list details: {ex.Message}");
                return Json(new { success = false, message = "Failed to get list details" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetEntryLists(int entryId)
        {
            try
            {
                var entry = await _crmService.GetEntryById(entryId);
                if (entry == null)
                {
                    return Json(new { success = false, message = "Entry not found" });
                }

                var lists = entry.CrmEntryLists.Select(el => el.CrmList).ToList();
                return Json(new { success = true, lists });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting entry lists: {ex.Message}");
                return Json(new { success = false, message = "Failed to get entry lists" });
            }
        }

        #endregion


    }
}
