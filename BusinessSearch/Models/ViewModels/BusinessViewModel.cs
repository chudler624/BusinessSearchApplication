using BusinessSearch.Models.WebsiteAnalysis;

namespace BusinessSearch.Models.ViewModels
{
    public class BusinessViewModel
    {
        public int Id { get; set; }  // CRM Entry ID
        public string? BusinessId { get; set; }
        public string? Name { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? FullAddress { get; set; }
        public double Rating { get; set; }
        public int ReviewCount { get; set; }
        public string? Type { get; set; }
        public string? PriceLevel { get; set; }
        public string? BusinessStatus { get; set; }
        public string? OpeningStatus { get; set; }
        public WebsiteAnalysisModel? WebsiteAnalysis { get; set; }
        public string? PlaceLink { get; set; }
        public string? Notes { get; set; }
        public string? Disposition { get; set; }
        public DateTime DateAdded { get; set; }
        public string? PhotoUrl { get; set; }
        public string? Facebook { get; set; }
        public string? Instagram { get; set; }
        public string? YelpUrl { get; set; }
        public ICollection<CrmList>? Lists { get; set; }
    }
}