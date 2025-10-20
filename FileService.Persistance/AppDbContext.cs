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
                e.Property(x => x.Key).IsRequired().HasMaxLength(512);
                e.Property(x => x.FileName).IsRequired().HasMaxLength(255);
                e.Property(x => x.OwnerUserId).IsRequired().HasMaxLength(128);
                e.HasIndex(x => new { x.OwnerUserId, x.Status, x.CreatedAtUtc });
            });
        }
    }
}
