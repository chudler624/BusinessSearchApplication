using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessSearch.Models.Organization
{
    public class OrganizationPermissions
    {
        public int Id { get; set; }

        [StringLength(450)]
        public string? UserId { get; set; }

        public int? OrganizationId { get; set; }  // Changed to nullable int

        public bool CanSearch { get; set; } = true;
        public bool CanViewHistory { get; set; } = true;
        public bool CanManageCrm { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastModifiedAt { get; set; }

        [StringLength(450)]
        public string? LastModifiedById { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [ForeignKey("OrganizationId")]
        public virtual OrganizationEntity? Organization { get; set; }

        [ForeignKey("LastModifiedById")]
        public virtual ApplicationUser? LastModifiedBy { get; set; }
    }
}