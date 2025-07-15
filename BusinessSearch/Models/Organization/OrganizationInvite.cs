using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessSearch.Models.Organization
{
    public class OrganizationInvite
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(8)]
        public string InviteCode { get; set; } = string.Empty;

        [Required]
        public int OrganizationId { get; set; }

        [ForeignKey("OrganizationId")]
        public OrganizationEntity? Organization { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; }

        [Required]
        public DateTime ExpiryDate { get; set; }

        [Required]
        public bool IsActive { get; set; }

        [Required]
        [StringLength(50)]
        public string Role { get; set; } = string.Empty;
    }
}