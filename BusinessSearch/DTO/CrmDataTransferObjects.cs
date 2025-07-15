using System.ComponentModel.DataAnnotations;

namespace BusinessSearch.DTOs
{
    public class CrmListDto
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string? Industry { get; set; }

        public int? AssignedToId { get; set; }

        public string? AssignedToName { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? LastModifiedDate { get; set; }

        public int EntryCount { get; set; }
    }

    public class CrmEntryDto
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string BusinessName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Name { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(255)]
        public string? Website { get; set; }

        [Range(0, 5)]
        public double? GoogleRating { get; set; }

        [StringLength(50)]
        public string? Industry { get; set; }

        [StringLength(50)]
        public string? Disposition { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        public DateTime DateAdded { get; set; }

        // Social media fields
        [StringLength(255)]
        public string? Facebook { get; set; }

        [StringLength(255)]
        public string? Instagram { get; set; }

        [StringLength(255)]
        public string? YelpUrl { get; set; }

        // Additional fields
        public int? ReviewCount { get; set; }

        [StringLength(50)]
        public string? BusinessStatus { get; set; }

        [StringLength(50)]
        public string? OpeningStatus { get; set; }

        [StringLength(500)]
        public string? PhotoUrl { get; set; }

        [StringLength(500)]
        public string? FullAddress { get; set; }

        // Navigation properties
        public List<int> ListIds { get; set; } = new List<int>();

        public bool IsInMultipleLists => ListIds.Count > 1;
    }

    public class CreateCrmEntryDto
    {
        public CrmEntryDto Entry { get; set; } = new();
        public int? SelectedListId { get; set; }
        public IEnumerable<CrmListDto> AvailableLists { get; set; } = new List<CrmListDto>();
    }

    public class EditCrmEntryDto
    {
        public CrmEntryDto Entry { get; set; } = new();
        public List<CrmListDto> AvailableLists { get; set; } = new();
        public List<int> SelectedListIds { get; set; } = new();
    }

    public class CrmListDetailDto
    {
        public CrmListDto List { get; set; } = new();
        public List<CrmEntryDto> Entries { get; set; } = new();
        public List<CrmListDto> AvailableLists { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
    }
}