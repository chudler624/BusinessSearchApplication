using BusinessSearch.Data;
using BusinessSearch.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BusinessSearch.Services
{
    public class CrmService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CrmService> _logger;
        private readonly IExecutionStrategy _strategy;
        private readonly IOrganizationFilterService _orgFilter;

        public CrmService(
            ApplicationDbContext context,
            ILogger<CrmService> logger,
            IOrganizationFilterService orgFilter)
        {
            _context = context;
            _logger = logger;
            _strategy = _context.Database.CreateExecutionStrategy();
            _orgFilter = orgFilter;
        }

        #region List Operations

        public async Task<List<CrmList>> GetAllLists()
        {
            try
            {
                // Apply organization filter first
                var baseQuery = _context.CrmLists.AsQueryable();
                baseQuery = _orgFilter.ApplyOrganizationFilter(baseQuery);

                // Then apply includes and ordering
                return await baseQuery
                    .Include(l => l.AssignedTo)
                    .Include(l => l.CrmEntryLists)
                    .OrderBy(l => l.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting all lists: {ex.Message}");
                throw;
            }
        }

        public async Task<CrmList?> GetListById(int id)
        {
            try
            {
                var query = _context.CrmLists
                    .Include(l => l.AssignedTo)
                    .Include(l => l.CrmEntryLists)
                        .ThenInclude(el => el.CrmEntry)
                    .Where(l => l.Id == id);

                query = _orgFilter.ApplyOrganizationFilter(query);
                return await query.FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting list {id}: {ex.Message}");
                throw;
            }
        }

        public async Task<CrmList> CreateList(CrmList list)
        {
            return await _strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    list.OrganizationId = await _orgFilter.GetCurrentOrganizationId();
                    list.CreatedDate = DateTime.UtcNow;
                    _context.CrmLists.Add(list);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return list;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError($"Error creating list: {ex.Message}");
                    throw;
                }
            });
        }

        public async Task UpdateList(CrmList list)
        {
            // Log the update request extensively
            _logger.LogInformation($"=== UpdateList STARTED for ID: {list.Id} ===");
            _logger.LogInformation($"List details: Name='{list.Name}', " +
                $"Description='{list.Description?.Substring(0, Math.Min(20, list.Description?.Length ?? 0))}...', " +
                $"Industry='{list.Industry}', AssignedToId='{list.AssignedToId}', " +
                $"OrganizationId='{list.OrganizationId}'");

            // Check for null values
            if (list == null)
            {
                _logger.LogError("UpdateList received null list object");
                throw new ArgumentNullException(nameof(list), "List cannot be null");
            }

            // Validate that we have the required fields
            if (string.IsNullOrWhiteSpace(list.Name))
            {
                _logger.LogError("UpdateList received empty list name");
                throw new ArgumentException("List name is required", nameof(list.Name));
            }

            // Make sure we're updating an existing list
            if (list.Id <= 0)
            {
                _logger.LogError($"UpdateList received invalid ID: {list.Id}");
                throw new ArgumentException("List ID must be a positive number", nameof(list.Id));
            }

            try
            {
                // Verify the entity exists in the database
                var existingList = await _context.CrmLists.FindAsync(list.Id);
                if (existingList == null)
                {
                    _logger.LogError($"List with ID {list.Id} not found in database");
                    throw new KeyNotFoundException($"List with ID {list.Id} not found");
                }

                _logger.LogInformation($"Found existing list in database: Name='{existingList.Name}'");

                // Update individual properties
                existingList.Name = list.Name;
                existingList.Description = list.Description;
                existingList.Industry = list.Industry;
                existingList.AssignedToId = list.AssignedToId;
                existingList.LastModifiedDate = DateTime.UtcNow;
                existingList.LastModifiedById = list.LastModifiedById;

                _logger.LogInformation($"Entity state before SaveChanges: {_context.Entry(existingList).State}");

                // Save changes directly on the retrieved entity
                await _context.SaveChangesAsync();

                _logger.LogInformation($"=== UpdateList COMPLETED for ID: {list.Id} ===");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError($"Concurrency error updating list ID {list.Id}: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError($"Database update error for list ID {list.Id}: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"General error updating list ID {list.Id}: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        public async Task DeleteList(int id)
        {
            await _strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var list = await GetListById(id);
                    if (list != null && await _orgFilter.IsUserInOrganization(list.OrganizationId ?? -1))
                    {
                        _context.CrmLists.Remove(list);
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError($"Error deleting list {id}: {ex.Message}");
                    throw;
                }
            });
        }

        public async Task MergeLists(int sourceListId, int? targetListId, bool createNew, string? newListName)
        {
            await _strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var sourceList = await GetListById(sourceListId);
                    if (sourceList == null || !await _orgFilter.IsUserInOrganization(sourceList.OrganizationId ?? -1))
                    {
                        throw new UnauthorizedAccessException("User does not have access to source list");
                    }

                    CrmList targetList;
                    var orgId = await _orgFilter.GetCurrentOrganizationId();

                    if (createNew)
                    {
                        targetList = new CrmList
                        {
                            Name = newListName ?? $"Merged List {DateTime.UtcNow:yyyyMMdd}",
                            Industry = sourceList.Industry,
                            CreatedDate = DateTime.UtcNow,
                            OrganizationId = orgId
                        };
                        _context.CrmLists.Add(targetList);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        targetList = await GetListById(targetListId.Value);
                        if (targetList == null || !await _orgFilter.IsUserInOrganization(targetList.OrganizationId ?? -1))
                        {
                            throw new UnauthorizedAccessException("User does not have access to target list");
                        }
                    }

                    var entriesToMerge = sourceList.CrmEntryLists.Select(el => el.CrmEntry).ToList();
                    foreach (var entry in entriesToMerge)
                    {
                        if (!targetList.CrmEntryLists.Any(el => el.CrmEntryId == entry.Id))
                        {
                            var entryList = new CrmEntryList
                            {
                                CrmEntryId = entry.Id,
                                CrmListId = targetList.Id,
                                DateAdded = DateTime.UtcNow
                            };
                            _context.Add(entryList);
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError($"Error merging lists: {ex.Message}");
                    throw;
                }
            });
        }

        #endregion

        #region Entry Operations

        public async Task<List<CrmEntry>> GetAllEntries()
        {
            try
            {
                // Apply organization filter first
                var baseQuery = _context.CrmEntries.AsQueryable();
                baseQuery = _orgFilter.ApplyOrganizationFilter(baseQuery);

                // Then apply includes and ordering
                return await baseQuery
                    .Include(e => e.CrmEntryLists)
                        .ThenInclude(el => el.CrmList)
                    .OrderByDescending(e => e.DateAdded)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting all entries: {ex.Message}");
                throw;
            }
        }

        public async Task<CrmEntry?> GetEntryById(int id)
        {
            try
            {
                var query = _context.CrmEntries
                    .Include(e => e.CrmEntryLists)
                        .ThenInclude(el => el.CrmList)
                    .Where(e => e.Id == id);

                query = _orgFilter.ApplyOrganizationFilter(query);
                return await query.FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting entry {id}: {ex.Message}");
                throw;
            }
        }

        public async Task<CrmEntry> AddEntry(CrmEntry entry, int? listId = null)
        {
            return await _strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    _logger.LogInformation($"=== Starting AddEntry ===");
                    _logger.LogInformation($"Entry details: BusinessName={entry.BusinessName}, ListId={listId}");

                    var orgId = await _orgFilter.GetCurrentOrganizationId();

                    if (listId.HasValue)
                    {
                        var list = await GetListById(listId.Value);
                        if (list == null || list.OrganizationId != orgId)
                        {
                            throw new UnauthorizedAccessException($"User does not have access to list {listId}");
                        }
                    }

                    entry.DateAdded = DateTime.UtcNow;
                    _context.CrmEntries.Add(entry);
                    await _context.SaveChangesAsync();

                    if (listId.HasValue)
                    {
                        var entryList = new CrmEntryList
                        {
                            CrmEntryId = entry.Id,
                            CrmListId = listId.Value,
                            DateAdded = DateTime.UtcNow
                        };
                        _context.Add(entryList);
                        await _context.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();
                    return entry;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError($"Error in AddEntry: {ex.Message}");
                    throw;
                }
            });
        }

        public async Task UpdateEntry(CrmEntry entry)
        {
            await _strategy.ExecuteAsync(async () =>
            {
                try
                {
                    var existingEntry = await _context.CrmEntries.FindAsync(entry.Id);
                    if (existingEntry == null)
                    {
                        throw new KeyNotFoundException($"Entry with ID {entry.Id} not found");
                    }

                    _logger.LogInformation($"Found existing entry: {existingEntry.Id} - {existingEntry.BusinessName}");

                    // Update individual properties
                    existingEntry.BusinessName = entry.BusinessName;
                    existingEntry.Phone = entry.Phone;
                    existingEntry.Email = entry.Email;
                    existingEntry.Website = entry.Website;
                    existingEntry.Industry = entry.Industry;
                    existingEntry.Disposition = entry.Disposition;
                    existingEntry.GoogleRating = entry.GoogleRating;
                    existingEntry.Notes = entry.Notes;

                    _logger.LogInformation($"Notes before save: {existingEntry.Notes?.Substring(0, Math.Min(50, existingEntry.Notes?.Length ?? 0))}...");

                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Entry updated successfully: {existingEntry.Id}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error updating entry: {ex.Message}");
                    _logger.LogError($"Stack trace: {ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                    }
                    throw;
                }
            });
        }

        public async Task UpdateEntryLists(int entryId, List<int> listIds)
        {
            await _strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var orgId = await _orgFilter.GetCurrentOrganizationId();

                    // Verify all lists belong to the organization
                    var lists = await _context.CrmLists
                        .Where(l => listIds.Contains(l.Id))
                        .ToListAsync();

                    if (lists.Any(l => l.OrganizationId != orgId))
                    {
                        throw new UnauthorizedAccessException("User does not have access to one or more lists");
                    }

                    var existingRelations = await _context.Set<CrmEntryList>()
                        .Where(el => el.CrmEntryId == entryId)
                        .ToListAsync();
                    _context.RemoveRange(existingRelations);

                    foreach (var listId in listIds)
                    {
                        var entryList = new CrmEntryList
                        {
                            CrmEntryId = entryId,
                            CrmListId = listId,
                            DateAdded = DateTime.UtcNow
                        };
                        _context.Add(entryList);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError($"Error updating entry lists: {ex.Message}");
                    throw;
                }
            });
        }

        public async Task DeleteEntry(int id)
        {
            await _strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var entry = await GetEntryById(id);
                    if (entry != null)
                    {
                        _context.CrmEntries.Remove(entry);
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError($"Error deleting entry {id}: {ex.Message}");
                    throw;
                }
            });
        }

        // Add this to your CrmService class
        public async Task<CrmList> CreateSimpleList(CrmList list)
        {
            try
            {
                _context.CrmLists.Add(list);
                await _context.SaveChangesAsync();
                return list;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating list: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }


        #endregion

        #region Bulk Operations

        public async Task MoveEntries(int[] entryIds, int sourceListId, int destinationListId)
        {
            await _strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var orgId = await _orgFilter.GetCurrentOrganizationId();

                    // Verify list ownership
                    var sourcelist = await GetListById(sourceListId);
                    var destList = await GetListById(destinationListId);

                    if (sourcelist?.OrganizationId != orgId || destList?.OrganizationId != orgId)
                    {
                        throw new UnauthorizedAccessException("User does not have access to one or more lists");
                    }

                    var sourceRelations = await _context.Set<CrmEntryList>()
                        .Where(el => el.CrmListId == sourceListId && entryIds.Contains(el.CrmEntryId))
                        .ToListAsync();
                    _context.RemoveRange(sourceRelations);

                    foreach (var entryId in entryIds)
                    {
                        var entryList = new CrmEntryList
                        {
                            CrmEntryId = entryId,
                            CrmListId = destinationListId,
                            DateAdded = DateTime.UtcNow
                        };
                        _context.Add(entryList);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError($"Error moving entries: {ex.Message}");
                    throw;
                }
            });
        }

        public async Task CopyEntries(int[] entryIds, int sourceListId, int destinationListId)
        {
            await _strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var orgId = await _orgFilter.GetCurrentOrganizationId();

                    // Verify list ownership
                    var sourcelist = await GetListById(sourceListId);
                    var destList = await GetListById(destinationListId);

                    if (sourcelist?.OrganizationId != orgId || destList?.OrganizationId != orgId)
                    {
                        throw new UnauthorizedAccessException("User does not have access to one or more lists");
                    }

                    foreach (var entryId in entryIds)
                    {
                        var exists = await _context.Set<CrmEntryList>()
                            .AnyAsync(el => el.CrmListId == destinationListId && el.CrmEntryId == entryId);

                        if (!exists)
                        {
                            var entryList = new CrmEntryList
                            {
                                CrmEntryId = entryId,
                                CrmListId = destinationListId,
                                DateAdded = DateTime.UtcNow
                            };
                            _context.Add(entryList);
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError($"Error copying entries: {ex.Message}");
                    throw;
                }
            });
        }

        public async Task DeleteEntriesFromList(int[] entryIds, int listId)
        {
            await _strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var list = await GetListById(listId);
                    if (list == null || !await _orgFilter.IsUserInOrganization(list.OrganizationId ?? -1))
                    {
                        throw new UnauthorizedAccessException("User does not have access to this list");
                    }

                    var relations = await _context.Set<CrmEntryList>()
                        .Where(el => el.CrmListId == listId && entryIds.Contains(el.CrmEntryId))
                        .ToListAsync();

                    foreach (var relation in relations)
                    {
                        var existsInOtherLists = await _context.Set<CrmEntryList>()
                            .AnyAsync(el => el.CrmEntryId == relation.CrmEntryId && el.CrmListId != listId);

                        if (!existsInOtherLists)
                        {
                            var entry = await _context.CrmEntries.FindAsync(relation.CrmEntryId);
                            if (entry != null)
                            {
                                _context.CrmEntries.Remove(entry);
                            }
                        }
                        else
                        {
                            _context.Remove(relation);
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError($"Error deleting entries from list: {ex.Message}");
                    throw;
                }
            });
        }

        #endregion

        #region Helper Methods

        public async Task<bool> EntryExistsInList(int entryId, int listId)
        {
            try
            {
                var list = await GetListById(listId);
                if (list == null) return false;

                return await _context.Set<CrmEntryList>()
                    .AnyAsync(el => el.CrmEntryId == entryId && el.CrmListId == listId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error checking if entry exists in list: {ex.Message}");
                throw;
            }
        }

        public async Task<List<CrmList>> GetListsContainingEntry(int entryId)
        {
            try
            {
                var query = _context.CrmLists
                    .Include(l => l.CrmEntryLists)
                    .Where(l => l.CrmEntryLists.Any(el => el.CrmEntryId == entryId));

                query = _orgFilter.ApplyOrganizationFilter(query);
                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting lists containing entry: {ex.Message}");
                throw;
            }
        }

        public async Task<Dictionary<string, int>> GetDispositionCounts(int listId)
        {
            try
            {
                var list = await GetListById(listId);
                if (list == null) return new Dictionary<string, int>();

                return list.CrmEntryLists
                    .Select(el => el.CrmEntry.Disposition)
                    .GroupBy(d => d)
                    .ToDictionary(g => g.Key, g => g.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting disposition counts: {ex.Message}");
                throw;
            }
        }

        public async Task<Dictionary<string, int>> GetIndustryCounts(int listId)
        {
            try
            {
                var list = await GetListById(listId);
                if (list == null) return new Dictionary<string, int>();

                return list.CrmEntryLists
                    .Select(el => el.CrmEntry.Industry)
                    .Where(i => !string.IsNullOrEmpty(i))
                    .GroupBy(i => i)
                    .ToDictionary(g => g.Key, g => g.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting industry counts: {ex.Message}");
                throw;
            }
        }

        public async Task<List<CrmEntry>> SearchEntries(
            int? listId = null,
            string? searchTerm = null,
            string? dispositionFilter = null,
            string? industryFilter = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            try
            {
                IQueryable<CrmEntry> query = _context.CrmEntries
                    .Include(e => e.CrmEntryLists)
                    .ThenInclude(el => el.CrmList);

                query = _orgFilter.ApplyOrganizationFilter(query);

                if (listId.HasValue)
                {
                    query = query.Where(e => e.CrmEntryLists.Any(el => el.CrmListId == listId));
                }

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    query = query.Where(e =>
                        e.BusinessName.ToLower().Contains(searchTerm) ||
                        (e.Email != null && e.Email.ToLower().Contains(searchTerm)) ||
                        (e.Phone != null && e.Phone.Contains(searchTerm)) ||
                        (e.Website != null && e.Website.ToLower().Contains(searchTerm)) ||
                        (e.Notes != null && e.Notes.ToLower().Contains(searchTerm))
                    );
                }

                if (!string.IsNullOrEmpty(dispositionFilter))
                {
                    query = query.Where(e => e.Disposition == dispositionFilter);
                }

                if (!string.IsNullOrEmpty(industryFilter))
                {
                    query = query.Where(e => e.Industry == industryFilter);
                }

                if (startDate.HasValue)
                {
                    query = query.Where(e => e.DateAdded >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(e => e.DateAdded <= endDate.Value);
                }

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error searching entries: {ex.Message}");
                throw;
            }
        }

        #endregion
    }
}