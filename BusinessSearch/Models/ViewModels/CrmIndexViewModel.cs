// Models/ViewModels/CrmIndexViewModel.cs
namespace BusinessSearch.Models.ViewModels
{
    public class CrmIndexViewModel
    {
        public IEnumerable<CrmEntry> Entries { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
    }
}
