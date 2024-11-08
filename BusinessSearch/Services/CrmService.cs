using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessSearch.Data;
using BusinessSearch.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BusinessSearch.Services
{
    public class CrmService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CrmService> _logger;

        public CrmService(ApplicationDbContext context, ILogger<CrmService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<CrmEntry>> GetAllEntries()
        {
            try
            {
                _logger.LogInformation("Getting all CRM entries");
                return await _context.CrmEntries.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting entries: {ex.Message}");
                throw;
            }
        }

        public async Task<CrmEntry?> GetEntryById(int id)
        {
            try
            {
                _logger.LogInformation($"Getting CRM entry with ID: {id}");
                return await _context.CrmEntries.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting entry {id}: {ex.Message}");
                throw;
            }
        }

        public async Task<CrmEntry> AddEntry(CrmEntry entry)
        {
            try
            {
                _logger.LogInformation($"Adding new CRM entry: {entry.BusinessName}");
                entry.DateAdded = DateTime.UtcNow;
                _context.CrmEntries.Add(entry);
                var result = await _context.SaveChangesAsync();
                _logger.LogInformation($"Added entry result: {result} records affected");
                return entry;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding entry: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateEntry(CrmEntry entry)
        {
            try
            {
                _logger.LogInformation($"Updating CRM entry {entry.Id}");
                var existingEntry = await _context.CrmEntries.FindAsync(entry.Id);
                if (existingEntry == null)
                {
                    _logger.LogError($"Entry with ID {entry.Id} not found");
                    throw new Exception($"Entry with ID {entry.Id} not found");
                }

                // Log the values
                _logger.LogInformation($"Old values - Name: {existingEntry.BusinessName}, Disposition: {existingEntry.Disposition}");
                _logger.LogInformation($"New values - Name: {entry.BusinessName}, Disposition: {entry.Disposition}");

                _context.Entry(existingEntry).CurrentValues.SetValues(entry);
                var result = await _context.SaveChangesAsync();
                _logger.LogInformation($"Update result: {result} records affected");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating entry: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteEntry(int id)
        {
            try
            {
                _logger.LogInformation($"Deleting CRM entry {id}");
                var entry = await _context.CrmEntries.FindAsync(id);
                if (entry != null)
                {
                    _context.CrmEntries.Remove(entry);
                    var result = await _context.SaveChangesAsync();
                    _logger.LogInformation($"Delete result: {result} records affected");
                }
                else
                {
                    _logger.LogWarning($"Entry {id} not found for deletion");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting entry {id}: {ex.Message}");
                throw;
            }
        }
    }
}