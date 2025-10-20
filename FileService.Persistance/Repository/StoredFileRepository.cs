using FileService.Core.Contracts;
using FileService.Core.Enum;
using FileService.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileService.Persistance.Repository
{
    public sealed class StoredFileRepository : EfRepository<StoredFile>, IStoredFileRepository
    {
        public StoredFileRepository(AppDbContext db) : base(db) { }

        public async Task<StoredFile?> GetByIdForOwnerAsync(Guid id, string ownerUserId, CancellationToken ct = default)
            => await _dbContext.StoredFiles.AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == id && f.OwnerUserId == ownerUserId, ct);

        public async Task<StoredFile?> GetByIdNotDeletedForOwnerAsync(Guid id, string ownerUserId, CancellationToken ct = default)
            => await _dbContext.StoredFiles
                .FirstOrDefaultAsync(f => f.Id == id && f.OwnerUserId == ownerUserId && f.Status != FileStatus.Deleted, ct);

        public async Task<int> CountAsync(string ownerUserId, string? search, string? contentType, CancellationToken ct = default)
        {
            var q = _dbContext.StoredFiles.AsNoTracking()
                .Where(f => f.OwnerUserId == ownerUserId && f.Status != FileStatus.Deleted);

            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(f => EF.Functions.ILike(f.FileName, $"%{search}%") || EF.Functions.ILike(f.Description ?? "", $"%{search}%"));

            if (!string.IsNullOrWhiteSpace(contentType))
                q = q.Where(f => f.ContentType == contentType);

            return await q.CountAsync(ct);
        }

        public async Task<List<StoredFile>> ListPagedAsync(
            string ownerUserId, int page, int pageSize, string? search, string? contentType, CancellationToken ct = default)
        {
            var q = _dbContext.StoredFiles.AsNoTracking()
                .Where(f => f.OwnerUserId == ownerUserId && f.Status != FileStatus.Deleted);

            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(f => EF.Functions.ILike(f.FileName, $"%{search}%") || EF.Functions.ILike(f.Description ?? "", $"%{search}%"));

            if (!string.IsNullOrWhiteSpace(contentType))
                q = q.Where(f => f.ContentType == contentType);

            return await q.OrderByDescending(f => f.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);
        }
    }
}
