using Microsoft.AspNetCore.Mvc;
using BusinessSearch.Models;
using BusinessSearch.Services;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BusinessSearch.Controllers
{
    public class CrmController : Controller
    {
        private readonly CrmService _crmService;
        private readonly ILogger<CrmController> _logger;

        public CrmController(CrmService crmService, ILogger<CrmController> logger)
        {
            _crmService = crmService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var entries = await _crmService.GetAllEntries();
            return View(entries);
        }

        public IActionResult Create()
        {
            _logger.LogInformation("Creating new CRM entry");
            return View(new CrmEntry { DateAdded = DateTime.UtcNow, Disposition = "New" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CrmEntry crmEntry)
        {
            try
            {
                _logger.LogInformation($"Attempting to create entry: {crmEntry.BusinessName}");

                if (ModelState.IsValid)
                {
                    crmEntry.DateAdded = DateTime.UtcNow;
                    await _crmService.AddEntry(crmEntry);
                    TempData["Success"] = "Entry created successfully";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    _logger.LogWarning("Invalid model state in Create");
                    foreach (var modelState in ModelState.Values)
                    {
                        foreach (var error in modelState.Errors)
                        {
                            _logger.LogWarning($"Model error: {error.ErrorMessage}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating entry: {ex.Message}");
                ModelState.AddModelError("", $"Error creating entry: {ex.Message}");
            }
            return View(crmEntry);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            _logger.LogInformation($"Getting entry {id} for edit");
            var crmEntry = await _crmService.GetEntryById(id);
            if (crmEntry == null)
            {
                _logger.LogWarning($"Entry {id} not found");
                return NotFound();
            }
            return View(crmEntry);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CrmEntry crmEntry)
        {
            try
            {
                _logger.LogInformation($"Attempting to update entry {id}");

                if (id != crmEntry.Id)
                {
                    _logger.LogWarning($"ID mismatch: {id} vs {crmEntry.Id}");
                    return NotFound();
                }

                if (ModelState.IsValid)
                {
                    var existingEntry = await _crmService.GetEntryById(id);
                    if (existingEntry == null)
                    {
                        _logger.LogWarning($"Entry {id} not found during update");
                        return NotFound();
                    }

                    // Preserve the original DateAdded
                    crmEntry.DateAdded = existingEntry.DateAdded;
                    await _crmService.UpdateEntry(crmEntry);

                    TempData["Success"] = "Entry updated successfully";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    _logger.LogWarning("Invalid model state in Edit");
                    foreach (var modelState in ModelState.Values)
                    {
                        foreach (var error in modelState.Errors)
                        {
                            _logger.LogWarning($"Model error: {error.ErrorMessage}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating entry: {ex.Message}");
                ModelState.AddModelError("", $"Error updating entry: {ex.Message}");
            }
            return View(crmEntry);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateDisposition([FromBody] DispositionUpdateModel model)
        {
            try
            {
                _logger.LogInformation($"Updating disposition for entry {model.Id} to {model.Disposition}");
                var entry = await _crmService.GetEntryById(model.Id);
                if (entry == null)
                {
                    _logger.LogWarning($"Entry {model.Id} not found for disposition update");
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
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                _logger.LogInformation($"Deleting entry {id}");
                await _crmService.DeleteEntry(id);
                return Json(new { success = true, message = "Entry deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting entry: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}