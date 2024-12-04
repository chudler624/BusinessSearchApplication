using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BusinessSearch.Models.Organization;

namespace BusinessSearch.Models
{
    public class TeamMember
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Role { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        public DateTime DateAdded { get; set; } = DateTime.UtcNow;

        public int? OrganizationId { get; set; }  // Make nullable

        [ForeignKey("OrganizationId")]
        public virtual OrganizationEntity? Organization { get; set; }

        // Navigation property
        public virtual ICollection<CrmList> AssignedLists { get; set; } = new List<CrmList>();
    }
}