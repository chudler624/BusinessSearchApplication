using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BusinessSearch.Models.Organization;

namespace BusinessSearch.Models.ViewModels
{
    public class OrganizationDetailsViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int UserCount { get; set; }
        public List<OrganizationMemberViewModel> Members { get; set; } = new();
        public List<OrganizationInviteViewModel> ActiveInvites { get; set; } = new();
        public bool IsCurrentUserAdmin { get; set; }
        public string? CurrentUserId { get; set; }
        public OrganizationPlan Plan { get; set; }
        public SearchUsageStatus SearchUsage { get; set; }
    }

    public class OrganizationSettingsViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Organization name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Organization name must be between 2 and 100 characters")]
        [Display(Name = "Organization Name")]
        public string Name { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        [Display(Name = "Created By")]
        public string? CreatedByName { get; set; }

        [Display(Name = "Current Plan")]
        public OrganizationPlan Plan { get; set; }

        [Display(Name = "Promo Code")]
        public string? PromoCode { get; set; }

        [Display(Name = "Require Approval for New Members")]
        public bool RequireApprovalForNewMembers { get; set; }

        [Display(Name = "Restrict Data Access by Team")]
        public bool RestrictDataAccess { get; set; }

        [Display(Name = "Organization Status")]
        public bool IsActive { get; set; }
    }

    public class OrganizationMemberViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime JoinedDate { get; set; }
        public OrganizationPermissions? Permissions { get; set; }
    }

    public class OrganizationInviteViewModel
    {
        public string InviteCode { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
    }

    public class NoOrganizationViewModel
    {
        public string? ReturnUrl { get; set; }
    }

    public class CreateOrganizationViewModel
    {
        [Required]
        [Display(Name = "Organization Name")]
        public string OrganizationName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Plan")]
        public OrganizationPlan Plan { get; set; }

        [Display(Name = "Promo Code")]
        public string? PromoCode { get; set; }

        public string? ReturnUrl { get; set; }
    }
}