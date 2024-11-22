﻿using BusinessSearch.Models;

namespace BusinessSearch.Models.ViewModels
{
    public class SearchHistoryViewModel
    {
        public IEnumerable<SavedSearch> Searches { get; set; }
        public PaginationInfo Pagination { get; set; }
        public SearchHistoryFilter Filter { get; set; }
        public string? CurrentSort { get; set; }
        public bool IsAscending { get; set; }
    }

    public class PaginationInfo
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    public class SearchHistoryFilter
    {
        public string? Industry { get; set; }
        public string? ZipCode { get; set; }
    }

    public class RecentSearchesViewModel
    {
        public IEnumerable<SavedSearch> RecentSearches { get; set; }
        public int DisplayCount { get; set; }
    }
}
