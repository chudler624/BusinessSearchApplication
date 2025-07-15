using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessSearch.Models
{
    [Table("CrmEntryLists")]
    public class CrmEntryList
    {
        [Required]
        public int CrmEntryId { get; set; }

        [Required]
        public int CrmListId { get; set; }

        public DateTime DateAdded { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("CrmEntryId")]
        public virtual CrmEntry CrmEntry { get; set; }

        [ForeignKey("CrmListId")]
        public virtual CrmList CrmList { get; set; }
    }
}
