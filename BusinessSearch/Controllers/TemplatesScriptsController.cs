using BusinessSearch.Authorization;
using BusinessSearch.Models;
using BusinessSearch.Models.ViewModels;
using BusinessSearch.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BusinessSearch.Controllers
{
    [Authorize]
    [RequireOrganization]
    public class TemplatesScriptsController : Controller
    {
        private readonly TemplatesScriptsService _templatesScriptsService;
        private readonly ILogger<TemplatesScriptsController> _logger;
        private readonly IOrganizationFilterService _orgFilter;

        public TemplatesScriptsController(
            TemplatesScriptsService templatesScriptsService,
            ILogger<TemplatesScriptsController> logger,
            IOrganizationFilterService orgFilter)
        {
            _templatesScriptsService = templatesScriptsService;
            _logger = logger;
            _orgFilter = orgFilter;
        }

        public async Task<IActionResult> Index(
            string? searchTerm = null,
            string? categoryFilter = null,
            string? typeFilter = null,
            string activeTab = "email-templates")
        {
            try
            {
                var emailTemplates = await _templatesScriptsService.SearchEmailTemplates(searchTerm, categoryFilter);
                var callScripts = await _templatesScriptsService.SearchCallScripts(searchTerm, typeFilter);

                var viewModel = new TemplatesScriptsViewModel
                {
                    EmailTemplates = emailTemplates,
                    CallScripts = callScripts,
                    SearchTerm = searchTerm,
                    CategoryFilter = categoryFilter,
                    TypeFilter = typeFilter,
                    ActiveTab = activeTab
                };

                return View(viewModel);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in TemplatesScripts Index: {ex.Message}");
                TempData["Error"] = "An error occurred while loading templates and scripts.";
                return View(new TemplatesScriptsViewModel());
            }
        }

        #region Email Templates

        public async Task<IActionResult> CreateEmailTemplate()
        {
            try
            {
                var categories = await _templatesScriptsService.GetEmailTemplateCategories();
                var viewModel = new CreateEmailTemplateViewModel
                {
                    AvailableCategories = categories
                };
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading create email template form: {ex.Message}");
                TempData["Error"] = "An error occurred while loading the form.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEmailTemplate(CreateEmailTemplateViewModel viewModel)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    viewModel.AvailableCategories = await _templatesScriptsService.GetEmailTemplateCategories();
                    return View(viewModel);
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                viewModel.Template.CreatedById = userId;
                viewModel.Template.LastModifiedById = userId;

                await _templatesScriptsService.CreateEmailTemplate(viewModel.Template);
                TempData["Success"] = "Email template created successfully.";
                return RedirectToAction(nameof(Index), new { activeTab = "email-templates" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating email template: {ex.Message}");
                ModelState.AddModelError("", "An error occurred while creating the template.");
                viewModel.AvailableCategories = await _templatesScriptsService.GetEmailTemplateCategories();
                return View(viewModel);
            }
        }

        public async Task<IActionResult> EditEmailTemplate(int id)
        {
            try
            {
                var template = await _templatesScriptsService.GetEmailTemplateById(id);
                if (template == null)
                {
                    return NotFound();
                }

                var categories = await _templatesScriptsService.GetEmailTemplateCategories();
                var viewModel = new EditEmailTemplateViewModel
                {
                    Template = template,
                    AvailableCategories = categories
                };

                return View(viewModel); // Changed this line - removed explicit view name
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading edit email template form: {ex.Message}");
                TempData["Error"] = "An error occurred while loading the template.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEmailTemplate(EditEmailTemplateViewModel viewModel)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    viewModel.AvailableCategories = await _templatesScriptsService.GetEmailTemplateCategories();
                    return View(viewModel);
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                viewModel.Template.LastModifiedById = userId;

                await _templatesScriptsService.UpdateEmailTemplate(viewModel.Template);
                TempData["Success"] = "Email template updated successfully.";
                return RedirectToAction(nameof(Index), new { activeTab = "email-templates" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating email template: {ex.Message}");
                ModelState.AddModelError("", "An error occurred while updating the template.");
                viewModel.AvailableCategories = await _templatesScriptsService.GetEmailTemplateCategories();
                return View(viewModel);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEmailTemplate(int id)
        {
            try
            {
                await _templatesScriptsService.DeleteEmailTemplate(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting email template: {ex.Message}");
                return Json(new { success = false, message = "Failed to delete template" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetEmailTemplate(int id)
        {
            try
            {
                var template = await _templatesScriptsService.GetEmailTemplateById(id);
                if (template == null)
                {
                    return NotFound();
                }

                return Json(new
                {
                    id = template.Id,
                    name = template.Name,
                    subject = template.Subject,
                    body = template.Body,
                    category = template.Category,
                    description = template.Description,
                    tags = template.Tags
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting email template: {ex.Message}");
                return Json(new { success = false, message = "Failed to load template" });
            }
        }

        #endregion

        #region Call Scripts

        public async Task<IActionResult> CreateCallScript()
        {
            try
            {
                var types = await _templatesScriptsService.GetCallScriptTypes();
                var industries = await _templatesScriptsService.GetCallScriptIndustries();

                var viewModel = new CreateCallScriptViewModel
                {
                    AvailableTypes = types,
                    AvailableIndustries = industries
                };
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading create call script form: {ex.Message}");
                TempData["Error"] = "An error occurred while loading the form.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCallScript(CreateCallScriptViewModel viewModel)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    viewModel.AvailableTypes = await _templatesScriptsService.GetCallScriptTypes();
                    viewModel.AvailableIndustries = await _templatesScriptsService.GetCallScriptIndustries();
                    return View(viewModel);
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                viewModel.Script.CreatedById = userId;
                viewModel.Script.LastModifiedById = userId;

                await _templatesScriptsService.CreateCallScript(viewModel.Script);
                TempData["Success"] = "Call script created successfully.";
                return RedirectToAction(nameof(Index), new { activeTab = "call-scripts" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating call script: {ex.Message}");
                ModelState.AddModelError("", "An error occurred while creating the script.");
                viewModel.AvailableTypes = await _templatesScriptsService.GetCallScriptTypes();
                viewModel.AvailableIndustries = await _templatesScriptsService.GetCallScriptIndustries();
                return View(viewModel);
            }
        }

        public async Task<IActionResult> EditCallScript(int id)
        {
            try
            {
                var script = await _templatesScriptsService.GetCallScriptById(id);
                if (script == null)
                {
                    return NotFound();
                }

                var types = await _templatesScriptsService.GetCallScriptTypes();
                var industries = await _templatesScriptsService.GetCallScriptIndustries();

                var viewModel = new EditCallScriptViewModel
                {
                    Script = script,
                    AvailableTypes = types,
                    AvailableIndustries = industries
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading edit call script form: {ex.Message}");
                TempData["Error"] = "An error occurred while loading the script.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCallScript(EditCallScriptViewModel viewModel)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    viewModel.AvailableTypes = await _templatesScriptsService.GetCallScriptTypes();
                    viewModel.AvailableIndustries = await _templatesScriptsService.GetCallScriptIndustries();
                    return View(viewModel);
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                viewModel.Script.LastModifiedById = userId;

                await _templatesScriptsService.UpdateCallScript(viewModel.Script);
                TempData["Success"] = "Call script updated successfully.";
                return RedirectToAction(nameof(Index), new { activeTab = "call-scripts" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating call script: {ex.Message}");
                ModelState.AddModelError("", "An error occurred while updating the script.");
                viewModel.AvailableTypes = await _templatesScriptsService.GetCallScriptTypes();
                viewModel.AvailableIndustries = await _templatesScriptsService.GetCallScriptIndustries();
                return View(viewModel);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCallScript(int id)
        {
            try
            {
                await _templatesScriptsService.DeleteCallScript(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting call script: {ex.Message}");
                return Json(new { success = false, message = "Failed to delete script" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCallScript(int id)
        {
            try
            {
                var script = await _templatesScriptsService.GetCallScriptById(id);
                if (script == null)
                {
                    return NotFound();
                }

                return Json(new
                {
                    id = script.Id,
                    name = script.Name,
                    content = script.Content,
                    scriptType = script.ScriptType,
                    industry = script.Industry,
                    description = script.Description,
                    tags = script.Tags,
                    estimatedDuration = script.EstimatedDuration
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting call script: {ex.Message}");
                return Json(new { success = false, message = "Failed to load script" });
            }
        }

        #endregion
    }
}