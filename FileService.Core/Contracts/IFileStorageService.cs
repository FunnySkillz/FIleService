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
        Task<(Guid id, Uri uploadUrl, string objectKey)> CreateForUploadAsync(
            string fileName,
            string contentType,
            long? expectedSizeBytes,
            string ownerUserId,
            CancellationToken ct = default);

        Task<Uri?> GetDownloadUrlAsync(Guid id, string requesterUserId, CancellationToken ct = default);

        Task<StoredFile?> GetMetadataAsync(Guid id, string requesterUserId, CancellationToken ct = default);

        Task<bool> UpdateMetadataAsync(Guid id, string requesterUserId, string? newFileName, string? description, CancellationToken ct = default);

        Task<bool> DeleteAsync(Guid id, string requesterUserId, CancellationToken ct = default);

        Task<IReadOnlyList<StoredFile>> ListAsync(string ownerUserId, int page = 1, int pageSize = 50, CancellationToken ct = default);
    }

}
