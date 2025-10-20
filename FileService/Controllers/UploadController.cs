using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using FileService.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FileService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Post(CreateSignedUrlRequest createSignedUrlRequest)
        {
            string bucketName = "aerashiftbucket";
            string key = Guid.NewGuid().ToString() + ".txt";
            RegionEndpoint bucketRegion = RegionEndpoint.USEast1;

            var client = new AmazonS3Client(bucketRegion);

            var putRequest = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = key,
                ContentBody = createSignedUrlRequest.Content
            };

            PutObjectResponse putObjectResponse = await client.PutObjectAsync(putRequest);

            GetPreSignedUrlRequest preSignedUrlRequest = new GetPreSignedUrlRequest
            {
                BucketName = bucketName,
                Key = key,
                Expires = DateTime.UtcNow.AddHours(createSignedUrlRequest.TimeToLiveInHours)
            };

            string preSignedUrl = client.GetPreSignedURL(preSignedUrlRequest);

            return Ok(preSignedUrl);
        }



    }
}
