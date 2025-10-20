public enum FileStatus { Pending, Uploaded, Deleted }

namespace FileService.Models
{
    public class StoredFiles
    {
        public Guid Id { get; set; }
        public string Key { get; set; } = default!;              // s3 object key
        public string FileName { get; set; } = default!;
        public string ContentType { get; set; } = "application/octet-stream";
        public long SizeBytes { get; set; }
        public string OwnerUserId { get; set; } = default!;
        public string? Description { get; set; }

        public FileStatus Status { get; set; } = FileStatus.Pending;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UploadedAtUtc { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
        public bool IsDeleted => Status == FileStatus.Deleted;
    }
}
