using System.Collections.Generic;
using BusinessSearch.Models;

namespace BusinessSearch.Models.ViewModels
{
    public class CrmViewModel
    {
        public List<CrmEntry>? Entries { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
    }
}
