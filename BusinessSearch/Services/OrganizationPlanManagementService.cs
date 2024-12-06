using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BusinessSearch.Data;
using BusinessSearch.Models.Organization;

namespace BusinessSearch.Services
{
    public interface IOrganizationPlanManagementService
    {
        Task<OrganizationPlan> GetCurrentPlanAsync(int organizationId);
        Task<bool> UpdatePlanAsync(OrganizationPlan newPlan, string? promoCode, int organizationId);
        Task<bool> ValidatePromoCodeAsync(string promoCode);
    }

    public class OrganizationPlanManagementService : IOrganizationPlanManagementService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrganizationPlanManagementService> _logger;
        private const string PATRIOT_PROMO_CODE = "PATRIOT";

        public OrganizationPlanManagementService(ApplicationDbContext context, ILogger<OrganizationPlanManagementService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<OrganizationPlan> GetCurrentPlanAsync(int organizationId)
        {
            var org = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (org == null)
                throw new ArgumentException($"Organization not found: {organizationId}");

            return org.Plan;
        }

        public async Task<bool> UpdatePlanAsync(OrganizationPlan newPlan, string? promoCode, int organizationId)
        {
            var org = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (org == null)
                throw new ArgumentException($"Organization not found: {organizationId}");

            // Validate promo code for unlimited plan
            if (newPlan == OrganizationPlan.Unlimited)
            {
                if (!await ValidatePromoCodeAsync(promoCode))
                {
                    _logger.LogWarning("Invalid promo code attempt for organization {OrganizationId}", organizationId);
                    return false;
                }
            }

            org.Plan = newPlan;
            org.PromoCode = promoCode;

            // Ensure next reset is set
            if (org.NextSearchReset == default)
            {
                org.NextSearchReset = DateTime.UtcNow.Date.AddDays(1);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated plan to {Plan} for organization {OrganizationId}", newPlan, organizationId);

            return true;
        }

        public async Task<bool> ValidatePromoCodeAsync(string promoCode)
        {
            if (string.IsNullOrEmpty(promoCode))
                return false;

            await Task.CompletedTask; // For async consistency
            return promoCode.Equals(PATRIOT_PROMO_CODE, StringComparison.OrdinalIgnoreCase);
        }
    }
}