using FileService.Core.Enum;
using FileService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileService.Core.Contracts
{
    public interface IStoredFileRepository : IRepository<StoredFile>
    {
        Task<StoredFile?> GetByIdForOwnerAsync(Guid id, string ownerUserId, CancellationToken ct = default);

        Task<StoredFile?> GetByIdNotDeletedForOwnerAsync(Guid id, string ownerUserId, CancellationToken ct = default);

        Task<int> CountAsync(string ownerUserId, string? search, string? contentType, CancellationToken ct = default);

        Task<List<StoredFile>> ListPagedAsync(
            string ownerUserId, int page, int pageSize, string? search, string? contentType, CancellationToken ct = default);

        Task<StoredFile?> GetByIdForTenantAsync(Guid id, string tenantId, CancellationToken ct = default);
        Task<StoredFile?> GetActiveByIdForTenantAsync(Guid id, string tenantId, CancellationToken ct = default);

        Task<int> CountAsync(string tenantId, string? ownerType, string? ownerId, string? category,
                             string? search, string? contentType, CancellationToken ct = default);

        Task<List<StoredFile>> ListPagedAsync(string tenantId, string? ownerType, string? ownerId, string? category,
                             int page, int pageSize, string? search, string? contentType, CancellationToken ct = default);

    }
}
