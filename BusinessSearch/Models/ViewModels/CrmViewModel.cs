using BusinessSearch.Models;
using BusinessSearch.Models.WebsiteAnalysis;

namespace BusinessSearch.Models.ViewModels
{
    public class CrmListIndexViewModel
    {
        public IEnumerable<CrmList> Lists { get; set; } = new List<CrmList>();
        public IEnumerable<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public string? SearchTerm { get; set; }
        public string? IndustryFilter { get; set; }
        public int? AssignedToFilter { get; set; }
        public string? SortColumn { get; set; }
        public string? SortDirection { get; set; }
    }

    public class CrmListViewModel
    {
        public CrmList List { get; set; }
        public IEnumerable<CrmEntry> Entries { get; set; } = new List<CrmEntry>();
        public IEnumerable<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();
        public IEnumerable<CrmList> AvailableLists { get; set; } = new List<CrmList>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public string? SearchTerm { get; set; }
        public string? DispositionFilter { get; set; }
        public string? SortColumn { get; set; }
        public string? SortDirection { get; set; }
    }

    public class CreateCrmEntryViewModel
    {
        public CrmEntry Entry { get; set; }
        public IEnumerable<CrmList> AvailableLists { get; set; } = new List<CrmList>();
        public int? SelectedListId { get; set; }
    }

    public class EditCrmEntryViewModel
    {
        public CrmEntry Entry { get; set; }
        public IEnumerable<CrmList> AvailableLists { get; set; } = new List<CrmList>();
        public List<int> CurrentListIds { get; set; } = new List<int>();
        public List<int> SelectedListIds { get; set; } = new List<int>();
    }

    public class MergeListsViewModel
    {
        public int SourceListId { get; set; }
        public int? TargetListId { get; set; }
        public bool CreateNewList { get; set; }
        public string? NewListName { get; set; }
        public IEnumerable<CrmList> AvailableLists { get; set; } = new List<CrmList>();
    }

    public class BulkOperationResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public int AffectedEntries { get; set; }
    }
}