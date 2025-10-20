using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileService.Core.DTO
{
    public sealed class InitUploadResponse
    {
        public Guid Id { get; set; }
        public string Key { get; set; } = default!;
        public string UploadUrl { get; set; } = default!;
        public DateTime ExpiresAtUtc { get; set; }
    }
}
