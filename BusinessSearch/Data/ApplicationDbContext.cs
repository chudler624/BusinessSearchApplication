using BusinessSearch.Models;
using BusinessSearch.Models.Organization;
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

        public DbSet<TeamMember> TeamMembers { get; set; }
        public DbSet<CrmEntry> CrmEntries { get; set; }
        public DbSet<SavedSearch> SavedSearches { get; set; }
        public DbSet<SavedBusinessResult> SavedBusinessResults { get; set; }
        public DbSet<CrmList> CrmLists { get; set; }
        public DbSet<CrmEntryList> CrmEntryLists { get; set; }
        public DbSet<OrganizationEntity> Organizations { get; set; }
        public DbSet<OrganizationPermissions> OrganizationPermissions { get; set; }
        public DbSet<OrganizationInvite> OrganizationInvites { get; set; }
        public DbSet<OrganizationSearchUsage> OrganizationSearchUsage { get; set; }
        public DbSet<EmailTemplate> EmailTemplates { get; set; }
        public DbSet<CallScript> CallScripts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Identity tables
            modelBuilder.Entity<ApplicationUser>().ToTable("Users");
            modelBuilder.Entity<IdentityRole>().ToTable("Roles");
            modelBuilder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
            modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
            modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
            modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
            modelBuilder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");

            // Configure CRM Entry List (Many-to-Many relationship)
            modelBuilder.Entity<CrmEntryList>(builder =>
            {
                builder.HasKey(el => new { el.CrmEntryId, el.CrmListId });
                builder.Property(e => e.DateAdded).HasDefaultValueSql("GETUTCDATE()");
            });

            // Configure CrmEntry
            modelBuilder.Entity<CrmEntry>(entity =>
            {
                entity.Property(e => e.BusinessName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Name).HasMaxLength(100);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.Website).HasMaxLength(255);
                entity.Property(e => e.Industry).HasMaxLength(50);
                entity.Property(e => e.Disposition).HasMaxLength(50);
                entity.Property(e => e.Notes).HasMaxLength(1000);
                entity.Property(e => e.PhotoUrl).HasMaxLength(500);
                entity.Property(e => e.Facebook).HasMaxLength(255);
                entity.Property(e => e.Instagram).HasMaxLength(255);
                entity.Property(e => e.YelpUrl).HasMaxLength(255);
                entity.Property(e => e.FullAddress).HasMaxLength(500);
            });

            // Configure ApplicationUser
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(u => u.FirstName).HasMaxLength(100).IsRequired(false);
                entity.Property(u => u.LastName).HasMaxLength(100).IsRequired(false);
                entity.Property(u => u.JobTitle).HasMaxLength(100);
                entity.Property(u => u.OrganizationId).IsRequired(false);

                entity.HasOne(u => u.Organization)
                    .WithMany(o => o.Users)
                    .HasForeignKey(u => u.OrganizationId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(u => u.TeamMember)
                    .WithOne()
                    .HasForeignKey<ApplicationUser>(u => u.TeamMemberId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure CrmList
            modelBuilder.Entity<CrmList>(entity =>
            {
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Industry).HasMaxLength(50);
                entity.Property(e => e.CreatedById).HasMaxLength(450).IsRequired(false);
                entity.Property(e => e.LastModifiedById).HasMaxLength(450);
                entity.Property(e => e.AssignedToId).HasMaxLength(450);
                entity.Property(e => e.OrganizationId).IsRequired(false);

                entity.HasOne(l => l.CreatedBy)
                    .WithMany(u => u.CreatedLists)
                    .HasForeignKey(l => l.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(l => l.AssignedTo)
                    .WithMany(u => u.AssignedLists)
                    .HasForeignKey(l => l.AssignedToId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(l => l.LastModifiedBy)
                    .WithMany()
                    .HasForeignKey(l => l.LastModifiedById)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(l => l.Organization)
                    .WithMany(o => o.CrmLists)
                    .HasForeignKey(l => l.OrganizationId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Organization
            modelBuilder.Entity<OrganizationEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.CreatedById).HasMaxLength(450).IsRequired(false);
                entity.Property(e => e.Plan).IsRequired();
                entity.Property(e => e.NextSearchReset).IsRequired()
                    .HasDefaultValueSql("DATEADD(day, 1, GETUTCDATE())");

                entity.HasOne(o => o.CreatedBy)
                    .WithMany(u => u.CreatedOrganizations)
                    .HasForeignKey(o => o.CreatedById)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Organization Permissions
            modelBuilder.Entity<OrganizationPermissions>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId)
                    .HasMaxLength(450)
                    .IsRequired(false);
                entity.Property(e => e.LastModifiedById)
                    .HasMaxLength(450)
                    .IsRequired(false);

                entity.HasOne(p => p.Organization)
                    .WithMany()
                    .HasForeignKey(p => p.OrganizationId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.User)
                    .WithOne(u => u.Permissions)
                    .HasForeignKey<OrganizationPermissions>(p => p.UserId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.LastModifiedBy)
                    .WithMany(u => u.ModifiedPermissions)
                    .HasForeignKey(p => p.LastModifiedById)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure SavedSearch
            modelBuilder.Entity<SavedSearch>(entity =>
            {
                entity.Property(e => e.UserId).IsRequired(false);
                entity.Property(e => e.OrganizationId).IsRequired(false);

                entity.HasOne(s => s.User)
                    .WithMany(u => u.SavedSearches)
                    .HasForeignKey(s => s.UserId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(s => s.Organization)
                    .WithMany(o => o.SavedSearches)
                    .HasForeignKey(s => s.OrganizationId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure OrganizationInvite
            modelBuilder.Entity<OrganizationInvite>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.InviteCode).IsRequired().HasMaxLength(8);
                entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CreatedDate).IsRequired();
                entity.Property(e => e.ExpiryDate).IsRequired();

                entity.HasOne(i => i.Organization)
                    .WithMany()
                    .HasForeignKey(i => i.OrganizationId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.InviteCode).IsUnique();
            });

            // Configure OrganizationSearchUsage
            modelBuilder.Entity<OrganizationSearchUsage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Date).IsRequired();
                entity.Property(e => e.Count).IsRequired();
                entity.Property(e => e.LastUpdated).IsRequired();

                entity.HasOne(u => u.Organization)
                    .WithMany(o => o.SearchUsage)
                    .HasForeignKey(u => u.OrganizationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.OrganizationId, e.Date });
            });

            // Configure EmailTemplate
            modelBuilder.Entity<EmailTemplate>(entity =>
            {
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Subject).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Body).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Category).HasMaxLength(50);
                entity.Property(e => e.Tags).HasMaxLength(100);
                entity.Property(e => e.CreatedById).HasMaxLength(450);
                entity.Property(e => e.LastModifiedById).HasMaxLength(450);
                entity.Property(e => e.OrganizationId).IsRequired(false);

                entity.HasOne(e => e.CreatedBy)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedById)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.LastModifiedBy)
                    .WithMany()
                    .HasForeignKey(e => e.LastModifiedById)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Organization)
                    .WithMany()
                    .HasForeignKey(e => e.OrganizationId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure CallScript
            modelBuilder.Entity<CallScript>(entity =>
            {
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.ScriptType).HasMaxLength(50);
                entity.Property(e => e.Industry).HasMaxLength(50);
                entity.Property(e => e.Tags).HasMaxLength(100);
                entity.Property(e => e.CreatedById).HasMaxLength(450);
                entity.Property(e => e.LastModifiedById).HasMaxLength(450);
                entity.Property(e => e.OrganizationId).IsRequired(false);

                entity.HasOne(e => e.CreatedBy)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedById)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.LastModifiedBy)
                    .WithMany()
                    .HasForeignKey(e => e.LastModifiedById)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Organization)
                    .WithMany()
                    .HasForeignKey(e => e.OrganizationId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}