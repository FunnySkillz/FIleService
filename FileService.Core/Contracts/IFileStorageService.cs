using FileService.Core.DTO;
using FileService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileService.Core.Contracts
{
    public interface IFileStorageService
    {
        /// Create (init upload) — returns presigned PUT URL
        Task<(Guid id, string key, Uri uploadUrl, DateTime expiresAtUtc)> InitUploadAsync(
            string ownerUserId,
            string fileName,
            string contentType,
            long? expectedSizeBytes,
            CancellationToken ct = default);

        // Mark upload complete (server verifies object and records size)
        Task<bool> FinalizeAsync(Guid id, string ownerUserId, CancellationToken ct = default);

        // Read single file metadata
        Task<StoredFile?> GetAsync(Guid id, string ownerUserId, CancellationToken ct = default);

        // Read list (paged) with optional search & contentType filters
        Task<PagedResult<StoredFile>> ListAsync(
            string ownerUserId,
            int page,
            int pageSize,
            string? search,
            string? contentType,
            CancellationToken ct = default);

        // Update metadata (display name / description)
        Task<bool> UpdateMetadataAsync(Guid id, string ownerUserId, string? newFileName, string? description, CancellationToken ct = default);

        // Delete (soft delete in DB + delete object in S3)
        Task<bool> DeleteAsync(Guid id, string ownerUserId, CancellationToken ct = default);

        // Generate presigned GET URL for download
        Task<Uri?> GetDownloadUrlAsync(Guid id, string ownerUserId, CancellationToken ct = default);

    }

}
