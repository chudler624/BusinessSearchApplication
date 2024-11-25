using System.Text.Json.Serialization;

namespace BusinessSearch.Models
{
    public class DispositionUpdateModel
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("disposition")]
        public string? Disposition { get; set; }
    }
}