using Microsoft.AspNetCore.Identity;

namespace BusinessSearch.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Link to TeamMember
        public int? TeamMemberId { get; set; }
        public virtual TeamMember? TeamMember { get; set; }

        // Additional fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;
    }
}