using FileService.Core.Contracts;
using FileService.Core.DTO;
using FileService.Core.Enum;
using FileService.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FileService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TenantFilesController : ControllerBase
    {
        private readonly IFileStorageService _svc;
        public TenantFilesController(IFileStorageService svc) => _svc = svc;

        [HttpPost("PresignUpload")]
        public async Task<ActionResult<InitUploadResponse>> PresignUpload(
                [FromRoute] string tenantId, [FromBody] InitUploadRequest req, CancellationToken ct)
        {
            if (!CanAccessOwner(req.OwnerType, req.OwnerId)) return Forbid();

            var userId = User.FindFirst("sub")!.Value;
            var result = await _svc.InitUploadAsync(
                tenantId, userId, req.OwnerType, req.OwnerId, req.Category,
                req.FileName, req.ContentType, req.ExpectedSizeBytes, req.Metadata, ct);

            return Ok(new InitUploadResponse
            {
                Id = result.id,
                Key = result.key,
                UploadUrl = result.uploadUrl.ToString(),
                ExpiresAtUtc = result.expiresAtUtc
            });
        }

        [HttpPatch("{id:guid}/FinalizeUpload")]
        public async Task<IActionResult> FinalizeUpload(string tenantId, Guid id, CancellationToken ct)
            => await _svc.FinalizeAsync(id, tenantId, ct) ? NoContent() : NotFound();

        [HttpGet("GetFiles")]
        public async Task<ActionResult<PagedResult<StoredFile>>> GetFiles(
        [FromRoute] string tenantId,
        [FromQuery] string? ownerType, [FromQuery] string? ownerId, [FromQuery] string? category,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null, [FromQuery] string? contentType = null,
        CancellationToken ct = default)
        {
            if (!string.IsNullOrWhiteSpace(ownerType) && ownerType.Trim().ToLowerInvariant() == "user"
                && !string.IsNullOrWhiteSpace(ownerId) && !CanAccessOwner("user", ownerId))
                return Forbid();

            return Ok(await _svc.ListAsync(tenantId,
                ownerType?.Trim().ToLowerInvariant(), ownerId, category,
                page, pageSize, search, contentType, ct));
        }

        [HttpGet("GetFileById/{id:guid}")]
        public async Task<ActionResult<StoredFile>> GetFileById(string tenantId, Guid id, CancellationToken ct)
        {
            var file = await _svc.GetAsync(id, tenantId, ct);
            return file is null ? NotFound() : Ok(file);
        }

        [HttpPatch("UpdateFileMetadata/{id:guid}")]
        public async Task<IActionResult> UpdateFileMetadata(string tenantId, Guid id, [FromBody] UpdateFileRequest req, CancellationToken ct)
            => await _svc.UpdateMetadataAsync(id, tenantId, req.NewFileName, req.Description, ct) ? NoContent() : NotFound();

        [HttpDelete("DeleteFileById/{id:guid}")]
        public async Task<IActionResult> DeleteFileById(string tenantId, Guid id, CancellationToken ct)
            => await _svc.DeleteAsync(id, tenantId, ct) ? NoContent() : NotFound();

        [HttpGet("PresignDownloadUrl/{id:guid}")]
        public async Task<IActionResult> PresignDownloadUrl(string tenantId, Guid id, CancellationToken ct)
        {
            var url = await _svc.GetDownloadUrlAsync(id, tenantId, ct);
            return url is null ? NotFound() : Ok(new { url = url.ToString() });
        }

        private bool CanAccessOwner(string ownerType, string ownerId)
        {
            if (User.IsInRole("TenantAdmin")) return true;
            if (ownerType.Trim().ToLowerInvariant() == "user")
                return User.FindFirst("sub")?.Value == ownerId;
            return true;
        }
    }
}
