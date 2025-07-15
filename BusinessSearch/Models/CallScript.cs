using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BusinessSearch.Models.Organization;

namespace BusinessSearch.Models
{
    public class CallScript
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Script name is required")]
        [StringLength(100)]
        [Display(Name = "Script Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Script content is required")]
        [DataType(DataType.MultilineText)]
        public string Content { get; set; } = string.Empty;

        [StringLength(50)]
        public string? ScriptType { get; set; } // Cold Call, Follow-up, Closing, etc.

        [StringLength(50)]
        public string? Industry { get; set; }

        [StringLength(100)]
        public string? Tags { get; set; }

        public bool IsActive { get; set; } = true;

        [Range(1, 60)]
        [Display(Name = "Estimated Duration (minutes)")]
        public int? EstimatedDuration { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;

        [StringLength(450)]
        public string? CreatedById { get; set; }

        [StringLength(450)]
        public string? LastModifiedById { get; set; }

        public int? OrganizationId { get; set; }

        // Navigation properties
        public virtual ApplicationUser? CreatedBy { get; set; }
        public virtual ApplicationUser? LastModifiedBy { get; set; }
        public virtual OrganizationEntity? Organization { get; set; }
    }
}