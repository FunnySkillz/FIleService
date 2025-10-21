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

        [Obsolete("Use tenant-based methods (GetByIdForTenantAsync / ListPagedAsync with tenantId).")]
        public async Task<StoredFile?> GetByIdForOwnerAsync(Guid id, string ownerUserId, CancellationToken ct = default)
            => await _dbContext.StoredFiles.AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == id && f.OwnerUserId == ownerUserId, ct);

        [Obsolete("Use tenant-based methods (GetByIdForTenantAsync / ListPagedAsync with tenantId).")]
        public async Task<StoredFile?> GetByIdNotDeletedForOwnerAsync(Guid id, string ownerUserId, CancellationToken ct = default)
            => await _dbContext.StoredFiles
                .FirstOrDefaultAsync(f => f.Id == id && f.OwnerUserId == ownerUserId && f.Status != FileStatus.Deleted, ct);

        public async Task<int> CountAsync(
            string tenantId, string? ownerType, string? ownerId, string? category,
            string? search, string? contentType, CancellationToken ct = default)
        {
            var q = BaseQuery(tenantId, ownerType, ownerId, category, search, contentType).AsNoTracking();
            return await q.CountAsync(ct);
        }

        public async Task<List<StoredFile>> ListPagedAsync(
            string tenantId, string? ownerType, string? ownerId, string? category,
            int page, int pageSize, string? search, string? contentType, CancellationToken ct = default)
        {
            var q = BaseQuery(tenantId, ownerType, ownerId, category, search, contentType).AsNoTracking();

            return await q.OrderByDescending(f => f.CreatedAtUtc)
                          .Skip((page - 1) * pageSize)
                          .Take(pageSize)
                          .ToListAsync(ct);
        }

        public Task<StoredFile?> GetByIdForTenantAsync(Guid id, string tenantId, CancellationToken ct = default)
            => _dbContext.StoredFiles.AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == id && f.TenantId == tenantId, ct);

        public async Task<StoredFile?> GetActiveByIdForTenantAsync(Guid id, string tenantId, CancellationToken ct = default)
            => await _dbContext.StoredFiles
                .FirstOrDefaultAsync(f => f.Id == id && f.TenantId == tenantId && f.Status != FileStatus.Deleted, ct);

        IQueryable<StoredFile> BaseQuery(string tenantId, string? ownerType, string? ownerId, string? category,
                                         string? search, string? contentType)
        {
            var q = _dbContext.StoredFiles.Where(f => f.TenantId == tenantId && f.Status != FileStatus.Deleted);

            if (!string.IsNullOrWhiteSpace(ownerType)) q = q.Where(f => f.OwnerType == ownerType);
            if (!string.IsNullOrWhiteSpace(ownerId)) q = q.Where(f => f.OwnerId == ownerId);
            if (!string.IsNullOrWhiteSpace(category)) q = q.Where(f => f.Category == category);
            if (!string.IsNullOrWhiteSpace(contentType)) q = q.Where(f => f.ContentType == contentType);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var pattern = $"%{search}%";
                q = q.Where(f => EF.Functions.ILike(f.FileName, pattern) ||
                                 EF.Functions.ILike(f.Description ?? string.Empty, pattern));
            }
            return q;
        }

    }
}
