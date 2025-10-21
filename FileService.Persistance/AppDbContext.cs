using FileService.Core.Enum;
using FileService.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileService.Persistance
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<StoredFile> StoredFiles => Set<StoredFile>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<StoredFile>(e =>
            {
                e.HasKey(x => x.Id);

                e.Property(x => x.TenantId).IsRequired().HasMaxLength(128);
                e.Property(x => x.OwnerType).IsRequired().HasMaxLength(32);     // string
                e.Property(x => x.OwnerId).IsRequired().HasMaxLength(128);
                e.Property(x => x.Category).HasMaxLength(64);

                e.Property(x => x.Key).IsRequired().HasMaxLength(512);
                e.Property(x => x.FileName).IsRequired().HasMaxLength(255);
                e.Property(x => x.ContentType).IsRequired().HasMaxLength(128);
                e.Property(x => x.CreatedByUserId).HasMaxLength(128);

                // JSON metadata (Postgres jsonb)
                e.Property(x => x.Metadata).HasColumnType("jsonb").IsRequired(false);

                // Listing hot path
                e.HasIndex(x => new { x.TenantId, x.OwnerType, x.OwnerId, x.Status, x.CreatedAtUtc });

                // Optional singleton (e.g., one logo/profile per owner), excluding deleted
                var deleted = (int)FileStatus.Deleted;
                e.HasIndex(x => new { x.TenantId, x.OwnerType, x.OwnerId, x.Category })
                 .IsUnique()
                 .HasFilter($"\"Status\" <> {deleted}");
            });
        }
    }
}
