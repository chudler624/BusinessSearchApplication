namespace BusinessSearch.Models
{
    public class CrmEntryList
    {
        public int CrmEntryId { get; set; }
        public int CrmListId { get; set; }
        public DateTime DateAdded { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual CrmEntry CrmEntry { get; set; }
        public virtual CrmList CrmList { get; set; }
    }
}
