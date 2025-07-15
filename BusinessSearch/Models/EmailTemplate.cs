using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BusinessSearch.Models.Organization;

namespace BusinessSearch.Models
{
    public class EmailTemplate
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Template name is required")]
        [StringLength(100)]
        [Display(Name = "Template Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Subject is required")]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email body is required")]
        [DataType(DataType.MultilineText)]
        public string Body { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Category { get; set; }

        [StringLength(100)]
        public string? Tags { get; set; }

        public bool IsActive { get; set; } = true;

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