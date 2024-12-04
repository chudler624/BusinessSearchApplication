using System.ComponentModel.DataAnnotations;

namespace BusinessSearch.Models.Organization
{
    public class OrganizationEntity
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public DateTime CreatedAt { get; set; }

        [Required]
        [StringLength(450)]
        public string CreatedById { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ApplicationUser CreatedBy { get; set; }
        public virtual ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
        public virtual ICollection<CrmList> CrmLists { get; set; } = new List<CrmList>();
        public virtual ICollection<SavedSearch> SavedSearches { get; set; } = new List<SavedSearch>();
    }
}