using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileService.Core.DTO
{
    public sealed class UpdateFileRequest
    {
        public string? NewFileName { get; set; }
        public string? Description { get; set; }
    }
}
