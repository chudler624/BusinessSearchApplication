using BusinessSearch.Models;
using BusinessSearch.DTOs;

namespace BusinessSearch.Extensions
{
    public static class DtoMappingExtension
    {
        public static CrmEntryDto ToDto(this CrmEntry entry)
        {
            if (entry == null) return null;

            return new CrmEntryDto
            {
                Id = entry.Id,
                BusinessName = entry.BusinessName,
                DateAdded = entry.DateAdded,
                Email = entry.Email,
                Phone = entry.Phone,
                Website = entry.Website,
                Industry = entry.Industry,
                Disposition = entry.Disposition,
                Notes = entry.Notes
            };
        }

        public static CrmEntry ToModel(this CrmEntryDto dto)
        {
            if (dto == null) return null;

            return new CrmEntry
            {
                Id = dto.Id,
                BusinessName = dto.BusinessName,
                DateAdded = dto.DateAdded,
                Email = dto.Email,
                Phone = dto.Phone,
                Website = dto.Website,
                Industry = dto.Industry,
                Disposition = dto.Disposition,
                Notes = dto.Notes
            };
        }

        public static CrmListDto ToDto(this CrmList list)
        {
            if (list == null) return null;

            return new CrmListDto
            {
                Id = list.Id,
                Name = list.Name,
                Description = list.Description,
                Industry = list.Industry,
                CreatedDate = list.CreatedDate,
                EntryCount = list.CrmEntryLists?.Count ?? 0
            };
        }

        public static string ToStringId(this int? value)
        {
            return value?.ToString();
        }

        public static int? ToNullableInt(this string value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            return int.TryParse(value, out int result) ? result : null;
        }
    }
}
