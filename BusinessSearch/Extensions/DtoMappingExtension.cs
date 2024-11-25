using BusinessSearch.DTOs;
using BusinessSearch.Models;

namespace BusinessSearch.Extensions
{
    public static class DtoMappingExtensions
    {
        public static CrmListDto ToDto(this CrmList model)
        {
            return new CrmListDto
            {
                Id = model.Id,
                Name = model.Name,
                Description = model.Description,
                Industry = model.Industry,
                AssignedToId = model.AssignedToId,
                AssignedToName = model.AssignedTo?.Name,
                CreatedDate = model.CreatedDate,
                LastModifiedDate = model.LastModifiedDate,
                EntryCount = model.EntryCount
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
                GoogleRating = model.GoogleRating,
                Industry = model.Industry,
                Disposition = model.Disposition,
                Notes = model.Notes,
                DateAdded = model.DateAdded,
                ListIds = model.CrmEntryLists.Select(el => el.CrmListId).ToList()
            };
        }

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
                GoogleRating = dto.GoogleRating,
                Industry = dto.Industry,
                Disposition = dto.Disposition,
                Notes = dto.Notes,
                DateAdded = dto.DateAdded
            };
        }
    }
}
