using Amazon.S3;
using Amazon.S3.Model;
using FileService.Core.Contracts;
using FileService.Core.DTO;
using FileService.Core.Enum;
using FileService.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileService.Core.Services
{
    public sealed class S3FileStorageService : IFileStorageService
    {
        private readonly IAmazonS3 _s3;
        private readonly IUnitOfWork _uow;
        private readonly AwsOptions _opts;
        private readonly TimeSpan _uploadTtl;
        private readonly TimeSpan _downloadTtl;
        private readonly string _bucket;

        public S3FileStorageService(IAmazonS3 s3, IUnitOfWork uow, IOptions<AwsOptions> opts)
        {
            _s3 = s3;
            _uow = uow;
            _opts = opts.Value;
            _bucket = _opts.S3.BucketName;
            _uploadTtl = TimeSpan.FromSeconds(_opts.Presign.UploadExpirySeconds);
            _downloadTtl = TimeSpan.FromSeconds(_opts.Presign.DownloadExpirySeconds);
        }

        public async Task<(Guid id, string key, Uri uploadUrl, DateTime expiresAtUtc)> InitUploadAsync(
            string ownerUserId, string fileName, string contentType, long? expectedSizeBytes, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("FileName required.", nameof(fileName));

            var safeName = SanitizeFileName(fileName);
            var id = Guid.NewGuid();
            var key = $"users/{ownerUserId}/{id}/{safeName}";

            var entity = new StoredFile
            {
                Id = id,
                Key = key,
                FileName = safeName,
                ContentType = contentType,
                SizeBytes = expectedSizeBytes ?? 0,
                OwnerUserId = ownerUserId,
                Status = FileStatus.Pending
            };

            await _uow.StoredFiles.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);

            var expires = DateTime.UtcNow.Add(_uploadTtl);
            var pre = new GetPreSignedUrlRequest
            {
                BucketName = _bucket,
                Key = key,
                Verb = HttpVerb.PUT,
                Expires = expires,
                ContentType = contentType
            };
            pre.Headers["x-amz-acl"] = "private";
            pre.Headers["x-amz-server-side-encryption"] = "AES256";

            var url = _s3.GetPreSignedURL(pre);
            return (id, key, new Uri(url), expires);
        }

        public async Task<bool> FinalizeAsync(Guid id, string ownerUserId, CancellationToken ct)
        {
            var file = await _uow.StoredFiles.GetByIdNotDeletedForOwnerAsync(id, ownerUserId, ct);
            if (file is null) return false;

            try
            {
                var meta = await _s3.GetObjectMetadataAsync(_bucket, file.Key, ct);
                file.SizeBytes = meta.ContentLength;
                file.Status = FileStatus.Uploaded;
                file.UploadedAtUtc = DateTime.UtcNow;
                _uow.StoredFiles.Update(file);
                await _uow.SaveChangesAsync(ct);
                return true;
            }
            catch (AmazonS3Exception)
            {
                return false;
            }
        }

        public Task<StoredFile?> GetAsync(Guid id, string ownerUserId, CancellationToken ct)
            => _uow.StoredFiles.GetByIdNotDeletedForOwnerAsync(id, ownerUserId, ct);

        public async Task<PagedResult<StoredFile>> ListAsync(string ownerUserId, int page, int pageSize, string? search, string? contentType, CancellationToken ct)
        {
            var total = await _uow.StoredFiles.CountAsync(ownerUserId, search, contentType, ct);
            var items = await _uow.StoredFiles.ListPagedAsync(ownerUserId, page, pageSize, search, contentType, ct);
            return new PagedResult<StoredFile>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                Total = total
            };
        }

        public async Task<bool> UpdateMetadataAsync(Guid id, string ownerUserId, string? newFileName, string? description, CancellationToken ct)
        {
            var file = await _uow.StoredFiles.GetByIdNotDeletedForOwnerAsync(id, ownerUserId, ct);
            if (file is null) return false;

            if (!string.IsNullOrWhiteSpace(newFileName))
                file.FileName = SanitizeFileName(newFileName);

            file.Description = description;
            _uow.StoredFiles.Update(file);
            await _uow.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id, string ownerUserId, CancellationToken ct)
        {
            var file = await _uow.StoredFiles.GetByIdNotDeletedForOwnerAsync(id, ownerUserId, ct);
            if (file is null) return false;

            file.Status = FileStatus.Deleted;
            file.DeletedAtUtc = DateTime.UtcNow;
            _uow.StoredFiles.Update(file);
            await _uow.SaveChangesAsync(ct);

            await _s3.DeleteObjectAsync(_bucket, file.Key, ct);
            return true;
        }

        public async Task<Uri?> GetDownloadUrlAsync(Guid id, string ownerUserId, CancellationToken ct)
        {
            var file = await _uow.StoredFiles.GetByIdForOwnerAsync(id, ownerUserId, ct);
            if (file is null || file.Status != FileStatus.Uploaded) return null;

            var expires = DateTime.UtcNow.Add(_downloadTtl);
            var pre = new GetPreSignedUrlRequest
            {
                BucketName = _bucket,
                Key = file.Key,
                Verb = HttpVerb.GET,
                Expires = expires,
                ResponseHeaderOverrides = new ResponseHeaderOverrides
                {
                    ContentDisposition = $"attachment; filename=\"{file.FileName}\"",
                    ContentType = file.ContentType
                }
            };
            var url = _s3.GetPreSignedURL(pre);
            return new Uri(url);
        }

        private static string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var parts = name.Split(invalid, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var safe = string.Join("_", parts);
            return string.IsNullOrWhiteSpace(safe) ? "file" : safe;
        }

        public Task<(Guid id, Uri uploadUrl, string objectKey)> CreateForUploadAsync(string fileName, string contentType, long? expectedSizeBytes, string ownerUserId, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<StoredFile?> GetMetadataAsync(Guid id, string requesterUserId, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<StoredFile>> ListAsync(string ownerUserId, int page = 1, int pageSize = 50, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }
    }
}
