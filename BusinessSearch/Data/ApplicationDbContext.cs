using Microsoft.EntityFrameworkCore;
using BusinessSearch.Models;

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
                entity.Property(e => e.BusinessName).HasMaxLength(100);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.Website).HasMaxLength(255);
                entity.Property(e => e.Industry).HasMaxLength(50);
                entity.Property(e => e.Disposition).HasMaxLength(50);
                entity.Property(e => e.Notes).HasMaxLength(1000);
            });
        }
    }
}