using System;
using System.ComponentModel.DataAnnotations;

namespace BusinessSearch.Models
{
    public class CrmEntry
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Business name is required")]
        [StringLength(100)]
        [Display(Name = "Business Name")]
        public string BusinessName { get; set; } = string.Empty;

        [Display(Name = "Contact Name")]
        [StringLength(100)]
        public string? Name { get; set; } = string.Empty;

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; } 

        [Display(Name = "Phone Number")]
        [StringLength(20)]
        public string? Phone { get; set; } 

        [Display(Name = "Company")]
        [StringLength(100)]
        public string? Company { get; set; } 

        [StringLength(255)]
        public string? Website { get; set; } 

        [Display(Name = "Google Rating")]
        [Range(0, 5)]
        public double? GoogleRating { get; set; }

        [StringLength(50)]
        public string? Industry { get; set; } 

        [StringLength(50)]
        public string? Disposition { get; set; } 

        [DataType(DataType.MultilineText)]
        [StringLength(1000)]
        public string? Notes { get; set; } 

        public DateTime DateAdded { get; set; } = DateTime.UtcNow;
    }
}