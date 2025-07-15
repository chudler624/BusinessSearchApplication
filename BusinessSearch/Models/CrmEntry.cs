using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        [StringLength(4000)] 
        public string? Notes { get; set; }

        public DateTime DateAdded { get; set; } = DateTime.UtcNow;

        [StringLength(500)]
        public string? PhotoUrl { get; set; }

        public int? ReviewCount { get; set; }

        [StringLength(50)]
        public string? BusinessStatus { get; set; }

        [StringLength(50)]
        public string? OpeningStatus { get; set; }

        [StringLength(255)]
        public string? Facebook { get; set; }

        [StringLength(255)]
        public string? Instagram { get; set; }

        [StringLength(255)]
        public string? YelpUrl { get; set; }

        [StringLength(500)]
        public string? FullAddress { get; set; }

        // Navigation properties
        public virtual ICollection<CrmEntryList> CrmEntryLists { get; set; } = new List<CrmEntryList>();

        [NotMapped]
        public bool IsInMultipleLists => CrmEntryLists?.Count > 1;
    }
}