using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileService.Core.DTO
{
    public sealed class AwsOptions
    {
        public string Region { get; set; } = "eu-central-1";
        public S3Options S3 { get; set; } = new();
        public PresignOptions Presign { get; set; } = new();

        public sealed class S3Options
        {
            public string BucketName { get; set; } = string.Empty;
        }
        public sealed class PresignOptions
        {
            public int UploadExpirySeconds { get; set; } = 600;
            public int DownloadExpirySeconds { get; set; } = 300;
        }
    }
}
