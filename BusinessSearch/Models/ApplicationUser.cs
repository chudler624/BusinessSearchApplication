using Microsoft.AspNetCore.Identity;
using BusinessSearch.Models.Organization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessSearch.Models
{
    public class ApplicationUser : IdentityUser
    {
        public ApplicationUser()
        {
            CreatedAt = DateTime.UtcNow;
            IsActive = true;
            CreatedLists = new List<CrmList>();
            AssignedLists = new List<CrmList>();
            SavedSearches = new List<SavedSearch>();
            CreatedOrganizations = new List<OrganizationEntity>();
            ModifiedPermissions = new List<OrganizationPermissions>();
        }

        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; }

        public int? TeamMemberId { get; set; }

        [ForeignKey("TeamMemberId")]
        public virtual TeamMember TeamMember { get; set; }

        public int? OrganizationId { get; set; }
        public OrganizationRole? OrganizationRole { get; set; }

        [StringLength(100)]
        public string FirstName { get; set; }

        [StringLength(100)]
        public string LastName { get; set; }

        [StringLength(100)]
        public string? JobTitle { get; set; }

        [ForeignKey("OrganizationId")]
        public virtual OrganizationEntity Organization { get; set; }

        public virtual OrganizationPermissions Permissions { get; set; }

        public virtual ICollection<CrmList> CreatedLists { get; set; }
        public virtual ICollection<CrmList> AssignedLists { get; set; }
        public virtual ICollection<SavedSearch> SavedSearches { get; set; }

        [InverseProperty("CreatedBy")]
        public virtual ICollection<OrganizationEntity> CreatedOrganizations { get; set; }

        [InverseProperty("LastModifiedBy")]
        public virtual ICollection<OrganizationPermissions> ModifiedPermissions { get; set; }

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";

        [NotMapped]
        public string Name => TeamMember?.Name ?? $"{FirstName} {LastName}";

        [NotMapped]
        public bool IsOrganizationAdmin
        {
            get
            {
                if (!OrganizationRole.HasValue) return false;
                return (int)OrganizationRole.Value == 0; // 0 is Admin in the enum
            }
        }

        [NotMapped]
        public bool BelongsToOrganization => OrganizationId.HasValue;

        public bool HasPermission(Func<OrganizationPermissions, bool> permissionCheck)
        {
            if (IsOrganizationAdmin) return true;
            return Permissions != null && permissionCheck(Permissions);
        }

        [NotMapped]
        public bool IsOrganizationCaller
        {
            get
            {
                if (!OrganizationRole.HasValue) return false;
                return (int)OrganizationRole.Value == 2; // 2 is Caller in the enum
            }
        }

        [NotMapped]
        public bool IsOrganizationMember
        {
            get
            {
                if (!OrganizationRole.HasValue) return false;
                return (int)OrganizationRole.Value == 1; // 1 is Member in the enum
            }
        }

        // Update existing permission methods to handle Caller role
        public bool CanSearchBusiness => IsOrganizationAdmin || IsOrganizationMember || HasPermission(p => p.CanSearch);
        public bool CanViewSearchHistory => IsOrganizationAdmin || IsOrganizationMember || HasPermission(p => p.CanViewHistory);
        public bool CanManageCrmData => IsOrganizationAdmin || IsOrganizationMember || HasPermission(p => p.CanManageCrm);

        // New method for Callers - only see assigned lists
        public bool CanOnlyViewAssignedLists => IsOrganizationCaller && !IsOrganizationAdmin && !IsOrganizationMember;

        
    }
}

