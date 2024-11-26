using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessSearch.Models
{
    public class SavedSearch
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Industry { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string ZipCode { get; set; } = string.Empty;

        [Required]
        public int ResultLimit { get; set; }

        [Required]
        public DateTime SearchDate { get; set; }

        public int TotalResults { get; set; }

        // Updated Identity reference
        [StringLength(450)]
        public string? UserId { get; set; }

        // Navigation properties
        public virtual ApplicationUser? User { get; set; }
        public virtual ICollection<SavedBusinessResult> Results { get; set; } = new List<SavedBusinessResult>();
    }

    public class SavedBusinessResult
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SavedSearchId { get; set; }

        // All the Business properties
        [MaxLength(100)]
        public string? BusinessId { get; set; }

        [MaxLength(100)]
        public string? Name { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        [MaxLength(500)]
        public string? FullAddress { get; set; }

        public int ReviewCount { get; set; }
        public double Rating { get; set; }

        [MaxLength(50)]
        public string? OpeningStatus { get; set; }

        [MaxLength(255)]
        public string? Website { get; set; }

        public bool Verified { get; set; }

        [MaxLength(255)]
        public string? PlaceLink { get; set; }

        [MaxLength(255)]
        public string? ReviewsLink { get; set; }

        [MaxLength(100)]
        public string? OwnerName { get; set; }

        [MaxLength(50)]
        public string? BusinessStatus { get; set; }

        [MaxLength(50)]
        public string? Type { get; set; }

        // Store as JSON string
        [MaxLength(500)]
        public string? SubtypesJson { get; set; }

        public int PhotoCount { get; set; }

        [MaxLength(20)]
        public string? PriceLevel { get; set; }

        [MaxLength(100)]
        public string? StreetAddress { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(10)]
        public string? Zipcode { get; set; }

        [MaxLength(50)]
        public string? State { get; set; }

        [MaxLength(50)]
        public string? Country { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(255)]
        public string? PhotoUrl { get; set; }

        [MaxLength(255)]
        public string? Facebook { get; set; }

        [MaxLength(255)]
        public string? Instagram { get; set; }

        [MaxLength(255)]
        public string? YelpUrl { get; set; }

        // Navigation property
        [ForeignKey("SavedSearchId")]
        public virtual SavedSearch SavedSearch { get; set; }
    }
}
