using BusinessSearch.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BusinessSearch.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<CrmEntry> CrmEntries { get; set; }
        public DbSet<SavedSearch> SavedSearches { get; set; }
        public DbSet<SavedBusinessResult> SavedBusinessResults { get; set; }
        public DbSet<CrmList> CrmLists { get; set; }
        public DbSet<TeamMember> TeamMembers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Must call base first to set up Identity tables
            base.OnModelCreating(modelBuilder);

            // Customize Identity table names (optional)
            modelBuilder.Entity<ApplicationUser>().ToTable("Users");
            modelBuilder.Entity<IdentityRole>().ToTable("Roles");
            modelBuilder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
            modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
            modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
            modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
            modelBuilder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");

            // Configure ApplicationUser
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.HasOne(u => u.TeamMember)
                    .WithOne()
                    .HasForeignKey<ApplicationUser>(u => u.TeamMemberId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<CrmEntry>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Required fields
                entity.Property(e => e.BusinessName)
                    .IsRequired()
                    .HasMaxLength(100);
                entity.Property(e => e.DateAdded)
                    .IsRequired();

                // Optional fields with max length
                entity.Property(e => e.Name)
                    .HasMaxLength(100);
                entity.Property(e => e.Email)
                    .HasMaxLength(100);
                entity.Property(e => e.Phone)
                    .HasMaxLength(20);
                entity.Property(e => e.Company)
                    .HasMaxLength(100);
                entity.Property(e => e.Website)
                    .HasMaxLength(255);
                entity.Property(e => e.Industry)
                    .HasMaxLength(50);
                entity.Property(e => e.Disposition)
                    .HasMaxLength(50);
                entity.Property(e => e.Notes)
                    .HasMaxLength(1000);

                // Additional fields
                entity.Property(e => e.PhotoUrl)
                    .HasMaxLength(500);
                entity.Property(e => e.Facebook)
                    .HasMaxLength(255);
                entity.Property(e => e.Instagram)
                    .HasMaxLength(255);
                entity.Property(e => e.YelpUrl)
                    .HasMaxLength(255);
                entity.Property(e => e.FullAddress)
                    .HasMaxLength(500);
                entity.Property(e => e.BusinessStatus)
                    .HasMaxLength(50);
                entity.Property(e => e.OpeningStatus)
                    .HasMaxLength(50);

                // Indexes
                entity.HasIndex(e => e.BusinessName);
                entity.HasIndex(e => e.DateAdded);
                entity.HasIndex(e => e.Disposition);
            });

            modelBuilder.Entity<CrmList>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);
                entity.Property(e => e.Description)
                    .HasMaxLength(500);
                entity.Property(e => e.Industry)
                    .HasMaxLength(50);
                entity.Property(e => e.CreatedDate)
                    .IsRequired();

                entity.HasOne(e => e.AssignedTo)
                    .WithMany(t => t.AssignedLists)
                    .HasForeignKey(e => e.AssignedToId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Add relationship to ApplicationUser for tracking who created the list
                entity.Property(e => e.CreatedById)
                    .HasMaxLength(450);  // Length for Identity user IDs
                entity.HasOne<ApplicationUser>()
                    .WithMany()
                    .HasForeignKey(e => e.CreatedById)
                    .OnDelete(DeleteBehavior.SetNull);

                // Indexes
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.Industry);
                entity.HasIndex(e => e.AssignedToId);
                entity.HasIndex(e => e.CreatedById);
            });

            modelBuilder.Entity<TeamMember>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);
                entity.Property(e => e.Role)
                    .HasMaxLength(100);
                entity.Property(e => e.Email)
                    .HasMaxLength(100);
                entity.Property(e => e.DateAdded)
                    .IsRequired();

                // Indexes
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.Role);
            });

            modelBuilder.Entity<CrmEntryList>(entity =>
            {
                entity.HasKey(e => new { e.CrmEntryId, e.CrmListId });

                entity.Property(e => e.DateAdded)
                    .IsRequired();

                entity.HasOne(el => el.CrmEntry)
                    .WithMany(e => e.CrmEntryLists)
                    .HasForeignKey(el => el.CrmEntryId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(el => el.CrmList)
                    .WithMany(l => l.CrmEntryLists)
                    .HasForeignKey(el => el.CrmListId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Index
                entity.HasIndex(e => new { e.CrmEntryId, e.CrmListId });
            });

            modelBuilder.Entity<SavedSearch>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Required fields
                entity.Property(e => e.Industry)
                    .IsRequired()
                    .HasMaxLength(100);
                entity.Property(e => e.ZipCode)
                    .IsRequired()
                    .HasMaxLength(10);
                entity.Property(e => e.ResultLimit)
                    .IsRequired();
                entity.Property(e => e.SearchDate)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.TotalResults)
                    .IsRequired();

                // Link to ApplicationUser instead of generic UserId
                entity.Property(e => e.UserId)
                    .HasMaxLength(450);
                entity.HasOne<ApplicationUser>()
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Configure one-to-many relationship with SavedBusinessResult
                entity.HasMany(e => e.Results)
                    .WithOne(e => e.SavedSearch)
                    .HasForeignKey(e => e.SavedSearchId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Indexes
                entity.HasIndex(e => e.SearchDate);
                entity.HasIndex(e => e.Industry);
                entity.HasIndex(e => e.ZipCode);
                entity.HasIndex(e => e.UserId);
            });

            modelBuilder.Entity<SavedBusinessResult>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Property configurations
                entity.Property(e => e.BusinessId)
                    .HasMaxLength(100);
                entity.Property(e => e.Name)
                    .HasMaxLength(100);
                entity.Property(e => e.PhoneNumber)
                    .HasMaxLength(20);
                entity.Property(e => e.FullAddress)
                    .HasMaxLength(500);
                entity.Property(e => e.OpeningStatus)
                    .HasMaxLength(50);
                entity.Property(e => e.Website)
                    .HasMaxLength(255);
                entity.Property(e => e.PlaceLink)
                    .HasMaxLength(255);
                entity.Property(e => e.ReviewsLink)
                    .HasMaxLength(255);
                entity.Property(e => e.OwnerName)
                    .HasMaxLength(100);
                entity.Property(e => e.BusinessStatus)
                    .HasMaxLength(50);
                entity.Property(e => e.Type)
                    .HasMaxLength(50);
                entity.Property(e => e.SubtypesJson)
                    .HasMaxLength(500);
                entity.Property(e => e.PriceLevel)
                    .HasMaxLength(20);
                entity.Property(e => e.StreetAddress)
                    .HasMaxLength(100);
                entity.Property(e => e.City)
                    .HasMaxLength(100);
                entity.Property(e => e.Zipcode)
                    .HasMaxLength(10);
                entity.Property(e => e.State)
                    .HasMaxLength(50);
                entity.Property(e => e.Country)
                    .HasMaxLength(50);
                entity.Property(e => e.Email)
                    .HasMaxLength(100);
                entity.Property(e => e.PhotoUrl)
                    .HasMaxLength(255);
                entity.Property(e => e.Facebook)
                    .HasMaxLength(255);
                entity.Property(e => e.Instagram)
                    .HasMaxLength(255);
                entity.Property(e => e.YelpUrl)
                    .HasMaxLength(255);

                // Indexes
                entity.HasIndex(e => e.SavedSearchId);
                entity.HasIndex(e => e.BusinessId);
                entity.HasIndex(e => e.Name);
            });
        }
    }
}