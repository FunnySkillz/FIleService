using FileService.Core.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileService.Core.DTO
{
    public sealed class InitUploadRequest
    {
        public string FileName { get; set; } = default!;
        public string ContentType { get; set; } = "application/octet-stream";
        public long? ExpectedSizeBytes { get; set; }

        public string OwnerType { get; set; } = default!; // "user" | "tenant" (lowercase expected)
        public string OwnerId { get; set; } = default!;
        public string? Category { get; set; }             // e.g. "profile","branding"
        public Dictionary<string, object>? Metadata { get; set; } // optional
    }
}
