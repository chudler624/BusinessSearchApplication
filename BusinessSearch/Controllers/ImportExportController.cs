// Controllers/ImportExportController.cs
using BusinessSearch.Authorization;
using BusinessSearch.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessSearch.Controllers
{
    [Authorize]
    [RequireOrganization]
    public class ImportExportController : Controller
    {
        private readonly IImportExportService _importExportService;
        private readonly CrmService _crmService;
        private readonly ILogger<ImportExportController> _logger;

        public ImportExportController(
            IImportExportService importExportService,
            CrmService crmService,
            ILogger<ImportExportController> logger)
        {
            _importExportService = importExportService;
            _crmService = crmService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Import(int listId)
        {
            var list = await _crmService.GetListById(listId);
            if (list == null)
            {
                return NotFound();
            }

            ViewBag.ListId = listId;
            ViewBag.ListName = list.Name;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(int listId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a file to upload";
                return RedirectToAction("Import", new { listId });
            }

            var list = await _crmService.GetListById(listId);
            if (list == null)
            {
                TempData["Error"] = "List not found";
                return RedirectToAction("Index", "Crm");
            }

            try
            {
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                (int created, int updated, List<string> errors) result;

                using (var stream = file.OpenReadStream())
                {
                    if (fileExtension == ".csv")
                    {
                        result = await _importExportService.ImportCrmEntriesFromCsv(stream, listId);
                    }
                    else if (fileExtension == ".xlsx" || fileExtension == ".xls")
                    {
                        result = await _importExportService.ImportCrmEntriesFromExcel(stream, listId);
                    }
                    else
                    {
                        TempData["Error"] = "Unsupported file format. Please upload a CSV or Excel file.";
                        return RedirectToAction("Import", new { listId });
                    }
                }

                // Check for errors
                if (result.errors.Any())
                {
                    ViewBag.ListId = listId;
                    ViewBag.ListName = list.Name;
                    ViewBag.ImportResult = result;
                    return View("ImportResult");
                }

                TempData["Success"] = $"Import successful. {result.created} entries created, {result.updated} entries updated.";
                return RedirectToAction("ListView", "Crm", new { id = listId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing file for list {ListId}", listId);
                TempData["Error"] = $"Error importing file: {ex.Message}";
                return RedirectToAction("Import", new { listId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportCsv(int listId)
        {
            try
            {
                var list = await _crmService.GetListById(listId);
                if (list == null)
                {
                    return NotFound();
                }

                var stream = await _importExportService.ExportListToCsv(listId);
                var fileName = $"{list.Name.Replace(" ", "_")}_Entries_{DateTime.Now:yyyyMMdd}.csv";

                return File(stream, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting CSV for list {ListId}", listId);
                TempData["Error"] = $"Error exporting file: {ex.Message}";
                return RedirectToAction("ListView", "Crm", new { id = listId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportExcel(int listId)
        {
            try
            {
                var list = await _crmService.GetListById(listId);
                if (list == null)
                {
                    return NotFound();
                }

                var stream = await _importExportService.ExportListToExcel(listId);
                var fileName = $"{list.Name.Replace(" ", "_")}_Entries_{DateTime.Now:yyyyMMdd}.xlsx";

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting Excel for list {ListId}", listId);
                TempData["Error"] = $"Error exporting file: {ex.Message}";
                return RedirectToAction("ListView", "Crm", new { id = listId });
            }
        }
    }
}