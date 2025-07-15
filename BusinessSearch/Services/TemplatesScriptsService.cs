using BusinessSearch.Data;
using BusinessSearch.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BusinessSearch.Services
{
    public class TemplatesScriptsService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TemplatesScriptsService> _logger;
        private readonly IExecutionStrategy _strategy;
        private readonly IOrganizationFilterService _orgFilter;

        public TemplatesScriptsService(
            ApplicationDbContext context,
            ILogger<TemplatesScriptsService> logger,
            IOrganizationFilterService orgFilter)
        {
            _context = context;
            _logger = logger;
            _strategy = _context.Database.CreateExecutionStrategy();
            _orgFilter = orgFilter;
        }

        #region Email Templates

        public async Task<List<EmailTemplate>> GetAllEmailTemplates()
        {
            try
            {
                var baseQuery = _context.EmailTemplates.AsQueryable();
                baseQuery = _orgFilter.ApplyOrganizationFilter(baseQuery);

                return await baseQuery
                    .Include(t => t.CreatedBy)
                    .Include(t => t.LastModifiedBy)
                    .Where(t => t.IsActive)
                    .OrderBy(t => t.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting email templates: {ex.Message}");
                throw;
            }
        }

        public async Task<EmailTemplate?> GetEmailTemplateById(int id)
        {
            try
            {
                var query = _context.EmailTemplates
                    .Include(t => t.CreatedBy)
                    .Include(t => t.LastModifiedBy)
                    .Where(t => t.Id == id);

                query = _orgFilter.ApplyOrganizationFilter(query);
                return await query.FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting email template {id}: {ex.Message}");
                throw;
            }
        }

        public async Task<EmailTemplate> CreateEmailTemplate(EmailTemplate template)
        {
            return await _strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    template.OrganizationId = await _orgFilter.GetCurrentOrganizationId();
                    template.CreatedDate = DateTime.UtcNow;
                    template.LastModifiedDate = DateTime.UtcNow;

                    _context.EmailTemplates.Add(template);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return template;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError($"Error creating email template: {ex.Message}");
                    throw;
                }
            });
        }

        public async Task UpdateEmailTemplate(EmailTemplate template)
        {
            await _strategy.ExecuteAsync(async () =>
            {
                try
                {
                    var existingTemplate = await _context.EmailTemplates.FindAsync(template.Id);
                    if (existingTemplate == null)
                    {
                        throw new KeyNotFoundException($"Email template with ID {template.Id} not found");
                    }

                    existingTemplate.Name = template.Name;
                    existingTemplate.Description = template.Description;
                    existingTemplate.Subject = template.Subject;
                    existingTemplate.Body = template.Body;
                    existingTemplate.Category = template.Category;
                    existingTemplate.Tags = template.Tags;
                    existingTemplate.IsActive = template.IsActive;
                    existingTemplate.LastModifiedDate = DateTime.UtcNow;
                    existingTemplate.LastModifiedById = template.LastModifiedById;

                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error updating email template: {ex.Message}");
                    throw;
                }
            });
        }

        public async Task DeleteEmailTemplate(int id)
        {
            await _strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var template = await GetEmailTemplateById(id);
                    if (template != null && await _orgFilter.IsUserInOrganization(template.OrganizationId ?? -1))
                    {
                        // Soft delete
                        template.IsActive = false;
                        template.LastModifiedDate = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError($"Error deleting email template {id}: {ex.Message}");
                    throw;
                }
            });
        }

        #endregion

        #region Call Scripts

        public async Task<List<CallScript>> GetAllCallScripts()
        {
            try
            {
                var baseQuery = _context.CallScripts.AsQueryable();
                baseQuery = _orgFilter.ApplyOrganizationFilter(baseQuery);

                return await baseQuery
                    .Include(s => s.CreatedBy)
                    .Include(s => s.LastModifiedBy)
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting call scripts: {ex.Message}");
                throw;
            }
        }

        public async Task<CallScript?> GetCallScriptById(int id)
        {
            try
            {
                var query = _context.CallScripts
                    .Include(s => s.CreatedBy)
                    .Include(s => s.LastModifiedBy)
                    .Where(s => s.Id == id);

                query = _orgFilter.ApplyOrganizationFilter(query);
                return await query.FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting call script {id}: {ex.Message}");
                throw;
            }
        }

        public async Task<CallScript> CreateCallScript(CallScript script)
        {
            return await _strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    script.OrganizationId = await _orgFilter.GetCurrentOrganizationId();
                    script.CreatedDate = DateTime.UtcNow;
                    script.LastModifiedDate = DateTime.UtcNow;

                    _context.CallScripts.Add(script);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return script;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError($"Error creating call script: {ex.Message}");
                    throw;
                }
            });
        }

        public async Task UpdateCallScript(CallScript script)
        {
            await _strategy.ExecuteAsync(async () =>
            {
                try
                {
                    var existingScript = await _context.CallScripts.FindAsync(script.Id);
                    if (existingScript == null)
                    {
                        throw new KeyNotFoundException($"Call script with ID {script.Id} not found");
                    }

                    existingScript.Name = script.Name;
                    existingScript.Description = script.Description;
                    existingScript.Content = script.Content;
                    existingScript.ScriptType = script.ScriptType;
                    existingScript.Industry = script.Industry;
                    existingScript.Tags = script.Tags;
                    existingScript.EstimatedDuration = script.EstimatedDuration;
                    existingScript.IsActive = script.IsActive;
                    existingScript.LastModifiedDate = DateTime.UtcNow;
                    existingScript.LastModifiedById = script.LastModifiedById;

                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error updating call script: {ex.Message}");
                    throw;
                }
            });
        }

        public async Task DeleteCallScript(int id)
        {
            await _strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var script = await GetCallScriptById(id);
                    if (script != null && await _orgFilter.IsUserInOrganization(script.OrganizationId ?? -1))
                    {
                        // Soft delete
                        script.IsActive = false;
                        script.LastModifiedDate = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError($"Error deleting call script {id}: {ex.Message}");
                    throw;
                }
            });
        }

        #endregion

        #region Helper Methods

        public async Task<List<string>> GetEmailTemplateCategories()
        {
            try
            {
                var baseQuery = _context.EmailTemplates.AsQueryable();
                baseQuery = _orgFilter.ApplyOrganizationFilter(baseQuery);

                return await baseQuery
                    .Where(t => t.IsActive && !string.IsNullOrEmpty(t.Category))
                    .Select(t => t.Category!)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting email template categories: {ex.Message}");
                throw;
            }
        }

        public async Task<List<string>> GetCallScriptTypes()
        {
            try
            {
                var baseQuery = _context.CallScripts.AsQueryable();
                baseQuery = _orgFilter.ApplyOrganizationFilter(baseQuery);

                return await baseQuery
                    .Where(s => s.IsActive && !string.IsNullOrEmpty(s.ScriptType))
                    .Select(s => s.ScriptType!)
                    .Distinct()
                    .OrderBy(t => t)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting call script types: {ex.Message}");
                throw;
            }
        }

        public async Task<List<string>> GetCallScriptIndustries()
        {
            try
            {
                var baseQuery = _context.CallScripts.AsQueryable();
                baseQuery = _orgFilter.ApplyOrganizationFilter(baseQuery);

                return await baseQuery
                    .Where(s => s.IsActive && !string.IsNullOrEmpty(s.Industry))
                    .Select(s => s.Industry!)
                    .Distinct()
                    .OrderBy(i => i)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting call script industries: {ex.Message}");
                throw;
            }
        }

        public async Task<List<EmailTemplate>> SearchEmailTemplates(
            string? searchTerm = null,
            string? categoryFilter = null)
        {
            try
            {
                var baseQuery = _context.EmailTemplates.AsQueryable();
                baseQuery = _orgFilter.ApplyOrganizationFilter(baseQuery);

                var query = baseQuery
                    .Include(t => t.CreatedBy)
                    .Where(t => t.IsActive);

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    query = query.Where(t =>
                        t.Name.ToLower().Contains(searchTerm) ||
                        (t.Description != null && t.Description.ToLower().Contains(searchTerm)) ||
                        (t.Subject != null && t.Subject.ToLower().Contains(searchTerm)) ||
                        (t.Tags != null && t.Tags.ToLower().Contains(searchTerm))
                    );
                }

                if (!string.IsNullOrEmpty(categoryFilter))
                {
                    query = query.Where(t => t.Category == categoryFilter);
                }

                return await query.OrderBy(t => t.Name).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error searching email templates: {ex.Message}");
                throw;
            }
        }

        public async Task<List<CallScript>> SearchCallScripts(
            string? searchTerm = null,
            string? typeFilter = null,
            string? industryFilter = null)
        {
            try
            {
                var baseQuery = _context.CallScripts.AsQueryable();
                baseQuery = _orgFilter.ApplyOrganizationFilter(baseQuery);

                var query = baseQuery
                    .Include(s => s.CreatedBy)
                    .Where(s => s.IsActive);

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    query = query.Where(s =>
                        s.Name.ToLower().Contains(searchTerm) ||
                        (s.Description != null && s.Description.ToLower().Contains(searchTerm)) ||
                        (s.Content != null && s.Content.ToLower().Contains(searchTerm)) ||
                        (s.Tags != null && s.Tags.ToLower().Contains(searchTerm))
                    );
                }

                if (!string.IsNullOrEmpty(typeFilter))
                {
                    query = query.Where(s => s.ScriptType == typeFilter);
                }

                if (!string.IsNullOrEmpty(industryFilter))
                {
                    query = query.Where(s => s.Industry == industryFilter);
                }

                return await query.OrderBy(s => s.Name).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error searching call scripts: {ex.Message}");
                throw;
            }
        }

        #endregion
    }
}