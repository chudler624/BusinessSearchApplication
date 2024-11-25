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

        public CrmService(ApplicationDbContext context, ILogger<CrmService> logger)
        {
            _context = context;
            _logger = logger;
            _strategy = _context.Database.CreateExecutionStrategy();
        }

        #region List Operations

        public async Task<List<CrmList>> GetAllLists()
        {
            try
            {
                return await _context.CrmLists
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
                return await _context.CrmLists
                    .Include(l => l.AssignedTo)
                    .Include(l => l.CrmEntryLists)
                        .ThenInclude(el => el.CrmEntry)
                    .FirstOrDefaultAsync(l => l.Id == id);
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
            try
            {
                list.LastModifiedDate = DateTime.UtcNow;
                _context.Entry(list).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating list: {ex.Message}");
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
                    var list = await _context.CrmLists.FindAsync(id);
                    if (list != null)
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
                    CrmList targetList;

                    if (createNew)
                    {
                        targetList = new CrmList
                        {
                            Name = newListName ?? $"Merged List {DateTime.UtcNow:yyyyMMdd}",
                            Industry = sourceList.Industry,
                            CreatedDate = DateTime.UtcNow
                        };
                        _context.CrmLists.Add(targetList);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        targetList = await GetListById(targetListId.Value);
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
                return await _context.CrmEntries
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
                return await _context.CrmEntries
                    .Include(e => e.CrmEntryLists)
                        .ThenInclude(el => el.CrmList)
                    .FirstOrDefaultAsync(e => e.Id == id);
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

                    if (listId.HasValue)
                    {
                        var listExists = await _context.CrmLists.AnyAsync(l => l.Id == listId.Value);
                        if (!listExists)
                        {
                            throw new Exception($"List with ID {listId} not found");
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
                    _context.Entry(entry).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error updating entry: {ex.Message}");
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
                    var entry = await _context.CrmEntries.FindAsync(id);
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

        #endregion

        #region Bulk Operations

        public async Task MoveEntries(int[] entryIds, int sourceListId, int destinationListId)
        {
            await _strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
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
                return await _context.CrmLists
                    .Include(l => l.CrmEntryLists)
                    .Where(l => l.CrmEntryLists.Any(el => el.CrmEntryId == entryId))
                    .ToListAsync();
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