namespace BusinessSearch.Models
{
    public class Business
    {
        public string? BusinessId { get; set; }
        public string? Name { get; set; }
        public string? PhoneNumber { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? FullAddress { get; set; }
        public int ReviewCount { get; set; }
        public double Rating { get; set; }
        public string? OpeningStatus { get; set; }
        public string? Website { get; set; }
        public bool Verified { get; set; }
        public string? PlaceLink { get; set; }
        public string? ReviewsLink { get; set; }
        public string? OwnerName { get; set; }
        public string? BusinessStatus { get; set; }
        public string? Type { get; set; }
        public List<string>? Subtypes { get; set; }
        public int PhotoCount { get; set; }
        public string? PriceLevel { get; set; }
        public string? StreetAddress { get; set; }
        public string? City { get; set; }
        public string? Zipcode { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
    }
}