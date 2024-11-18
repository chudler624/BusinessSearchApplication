using BusinessSearch.Models;
using Microsoft.EntityFrameworkCore;

namespace BusinessSearch.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<CrmEntry> CrmEntries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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

                // New fields
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
        }
    }
}