using BusinessSearch.DTOs;
using BusinessSearch.Models;

namespace BusinessSearch.Extensions
{
    public static class DtoExtensions
    {
        public static CrmEntry ToModel(this CrmEntryDto dto)
        {
            return new CrmEntry
            {
                Id = dto.Id,
                BusinessName = dto.BusinessName,
                Name = dto.Name,
                Email = dto.Email,
                Phone = dto.Phone,
                Website = dto.Website,
                Industry = dto.Industry,
                Disposition = dto.Disposition,
                Notes = dto.Notes,
                DateAdded = dto.DateAdded,
                FullAddress = dto.FullAddress,

                // Social media fields
                Facebook = dto.Facebook,
                Instagram = dto.Instagram,
                YelpUrl = dto.YelpUrl,

                // Rating and review fields
                GoogleRating = dto.GoogleRating,
                ReviewCount = dto.ReviewCount,

                // Status fields
                BusinessStatus = dto.BusinessStatus,
                OpeningStatus = dto.OpeningStatus,

                // Other fields
                PhotoUrl = dto.PhotoUrl
            };
        }

        public static CrmEntryDto ToDto(this CrmEntry model)
        {
            return new CrmEntryDto
            {
                Id = model.Id,
                BusinessName = model.BusinessName,
                Name = model.Name,
                Email = model.Email,
                Phone = model.Phone,
                Website = model.Website,
                Industry = model.Industry,
                Disposition = model.Disposition,
                Notes = model.Notes,
                DateAdded = model.DateAdded,
                FullAddress = model.FullAddress,

                // Social media fields
                Facebook = model.Facebook,
                Instagram = model.Instagram,
                YelpUrl = model.YelpUrl,

                // Rating and review fields
                GoogleRating = model.GoogleRating,
                ReviewCount = model.ReviewCount,

                // Status fields
                BusinessStatus = model.BusinessStatus,
                OpeningStatus = model.OpeningStatus,

                // Other fields
                PhotoUrl = model.PhotoUrl,

                // Set ListIds from CrmEntryLists
                ListIds = model.CrmEntryLists?.Select(el => el.CrmListId).ToList() ?? new List<int>()
            };
        }

        public static CrmListDto ToDto(this CrmList model)
        {
            return new CrmListDto
            {
                Id = model.Id,
                Name = model.Name,
                Description = model.Description,
                Industry = model.Industry,
                AssignedToId = model.AssignedToId != null ? int.Parse(model.AssignedToId) : null,
                AssignedToName = model.AssignedTo?.Name,
                CreatedDate = model.CreatedDate,
                LastModifiedDate = model.LastModifiedDate,
                EntryCount = model.EntryCount
            };
        }

        public static CrmList ToModel(this CrmListDto dto)
        {
            return new CrmList
            {
                Id = dto.Id,
                Name = dto.Name,
                Description = dto.Description,
                Industry = dto.Industry,
                AssignedToId = dto.AssignedToId?.ToString(),
                CreatedDate = dto.CreatedDate,
                LastModifiedDate = dto.LastModifiedDate
                // EntryCount is computed, so we don't set it here
            };
        }
    }
}