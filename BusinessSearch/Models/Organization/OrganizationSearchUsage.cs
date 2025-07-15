using System;
using System.ComponentModel.DataAnnotations;

namespace BusinessSearch.Models.Organization
{
    public class OrganizationSearchUsage
    {
        public int Id { get; set; }

        public int OrganizationId { get; set; }

        [Required]
        public DateTime Date { get; set; }

        public int Count { get; set; }

        public DateTime LastUpdated { get; set; }

        // Navigation property
        public virtual OrganizationEntity Organization { get; set; }
        public int ResultsCount { get; set; }
    }
}