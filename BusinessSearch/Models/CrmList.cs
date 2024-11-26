using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessSearch.Models
{
    public class CrmList
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "List Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string? Industry { get; set; }

        [Display(Name = "Assigned To")]
        public int? AssignedToId { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? LastModifiedDate { get; set; }

        // New Identity-related fields
        [StringLength(450)]
        public string? CreatedById { get; set; }

        [StringLength(450)]
        public string? LastModifiedById { get; set; }

        [NotMapped]
        public int EntryCount => CrmEntryLists?.Count ?? 0;

        // Navigation properties
        public virtual TeamMember? AssignedTo { get; set; }
        public virtual ApplicationUser? CreatedBy { get; set; }
        public virtual ApplicationUser? LastModifiedBy { get; set; }
        public virtual ICollection<CrmEntryList> CrmEntryLists { get; set; } = new List<CrmEntryList>();
    }
}
