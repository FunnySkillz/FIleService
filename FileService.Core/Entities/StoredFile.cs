

using FileService.Core.Enum;

namespace FileService.Models
{
    public class StoredFile
    {
        public Guid Id { get; set; }

        // Multi-tenant + ownership (OwnerType now string)
        public string TenantId { get; set; } = default!;
        public string OwnerType { get; set; } = default!;   // "user" | "tenant" (lowercase)
        public string OwnerId { get; set; } = default!;
        public string? Category { get; set; }               // e.g. "profile","branding","contracts"

        // File info
        public string Key { get; set; } = default!;         // S3 object key
        public string FileName { get; set; } = default!;
        public string ContentType { get; set; } = "application/octet-stream";
        public long SizeBytes { get; set; }

        // Auditing
        public string? CreatedByUserId { get; set; }        // nullable to allow system uploads
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UploadedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public DateTime? DeletedAtUtc { get; set; }

        // Optional metadata (JSON)
        public Dictionary<string, object>? Metadata { get; set; }
        public string? Description { get; set; } = "-";
        public FileStatus Status { get; set; } = FileStatus.Pending;
    }
}
