using FileService.Core.DTO;
using FileService.Core.Enum;
using FileService.Models;

namespace FileService.Core.Contracts
{
    public interface IFileStorageService
    {
        /// <summary>
        /// Create (init upload) — returns presigned PUT URL
        /// </summary>
        /// <param name="ownerUserId"></param>
        /// <param name="fileName"></param>
        /// <param name="contentType"></param>
        /// <param name="expectedSizeBytes"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<(Guid id, string key, Uri uploadUrl, DateTime expiresAtUtc)> InitUploadAsync(
            string tenantId,
            string createdByUserId,
            string ownerType,                 // "user" | "tenant"
            string ownerId,
            string? category,
            string fileName,
            string contentType,
            long? expectedSizeBytes,
            Dictionary<string, object>? metadata,
            CancellationToken ct = default);

        /// <summary>
        /// Mark upload complete (server verifies object and records size)
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ownerUserId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<bool> FinalizeAsync(Guid id, string tenantId, CancellationToken ct = default);

        /// <summary>
        /// Read single file metadata
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ownerUserId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<StoredFile?> GetAsync(Guid id, string tenantId, CancellationToken ct = default);

        /// <summary>
        /// Read list (paged) with optional search 
        /// </summary>
        /// <param name="ownerUserId"></param>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <param name="search"></param>
        /// <param name="contentType"></param>
        /// <param name="ct"></param>
        /// <returns></returns>& contentType filters
        Task<PagedResult<StoredFile>> ListAsync(
            string tenantId,
            string? ownerType, string? ownerId, string? category,
            int page, int pageSize, string? search, string? contentType, CancellationToken ct = default);



        /// <summary>
        /// Update metadata (display name / description)
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ownerUserId"></param>
        /// <param name="newFileName"></param>
        /// <param name="description"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<bool> UpdateMetadataAsync(Guid id, string tenantId, string? newFileName, string? description, CancellationToken ct = default);

        /// <summary>
        /// Delete (soft delete in DB + delete object in S3)
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ownerUserId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<bool> DeleteAsync(Guid id, string tenantId, CancellationToken ct = default);

        /// <summary>
        /// Generate presigned GET URL for download
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ownerUserId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Uri?> GetDownloadUrlAsync(Guid id, string tenantId, CancellationToken ct = default);
    }

}
