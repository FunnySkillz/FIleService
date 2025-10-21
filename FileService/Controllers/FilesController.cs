using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using FileService.Core.Contracts;
using FileService.Core.DTO;
using FileService.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FileService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        private readonly IFileStorageService _svc;

        public FilesController(IFileStorageService svc) => _svc = svc;

        //[HttpPost("PresignUpload")]
        //public async Task<ActionResult<InitUploadResponse>> InitUpload([FromBody] InitUploadRequest req, CancellationToken ct)
        //{
        //    var userId = GetUserIdOrThrow();
        //    var (id, key, url, expires) = await _svc.InitUploadAsync(userId, req.FileName, req.ContentType, req.ExpectedSizeBytes, ct);
        //    return Ok(new InitUploadResponse { Id = id, Key = key, UploadUrl = url.ToString(), ExpiresAtUtc = expires });
        //}

        [HttpPatch("{id:guid}/FinalizeUpload")]
        public async Task<IActionResult> Finalize(Guid id, CancellationToken ct)
        {
            var userId = GetUserIdOrThrow();
            var ok = await _svc.FinalizeAsync(id, userId, ct);
            return ok ? NoContent() : NotFound();
        }

        //[HttpGet("GetAllFilesByUserId")]
        //public async Task<ActionResult<PagedResult<StoredFile>>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        //    [FromQuery] string? search = null, [FromQuery] string? contentType = null, CancellationToken ct = default)
        //{
        //    var userId = GetUserIdOrThrow();
        //    var result = await _svc.ListAsync(userId, page, pageSize, search, contentType, ct);
        //    return Ok(result);
        //}

        [HttpGet("GetFileById/{id:guid}")]
        public async Task<ActionResult<StoredFile>> Get(Guid id, CancellationToken ct)
        {
            var userId = GetUserIdOrThrow();
            var file = await _svc.GetAsync(id, userId, ct);
            return file is null ? NotFound() : Ok(file);
        }

        [HttpPatch("UpdateFileMetadata/{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFileRequest req, CancellationToken ct)
        {
            var userId = GetUserIdOrThrow();
            var ok = await _svc.UpdateMetadataAsync(id, userId, req.NewFileName, req.Description, ct);
            return ok ? NoContent() : NotFound();
        }

        [HttpDelete("DeleteFileById/{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var userId = GetUserIdOrThrow();
            var ok = await _svc.DeleteAsync(id, userId, ct);
            return ok ? NoContent() : NotFound();
        }

        [HttpGet("PresignDownloadUrl/{id:guid}")]
        public async Task<IActionResult> GetDownloadUrl(Guid id, CancellationToken ct)
        {
            var userId = GetUserIdOrThrow();
            var url = await _svc.GetDownloadUrlAsync(id, userId, ct);
            return url is null ? NotFound() : Ok(new { url = url.ToString() });
        }

        private string GetUserIdOrThrow()
        {
            // Adjust to auth: "sub" for OIDC/JWT; or map to app's user id.
            var sub = User.FindFirst("JWT")?.Value;
            if (string.IsNullOrEmpty(sub)) throw new UnauthorizedAccessException("No user id (sub) in token.");
            return sub;
        }

    }
}
