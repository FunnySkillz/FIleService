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
    }
}
