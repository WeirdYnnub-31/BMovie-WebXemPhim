using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using webxemphim.Services;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace webxemphim.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadController : ControllerBase
    {
        private readonly BlobStorageService _blob;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<UploadController> _logger;
        private const long MaxVideoFileSize = 2L * 1024 * 1024 * 1024; // 2GB
        
        public UploadController(BlobStorageService blob, IWebHostEnvironment env, ILogger<UploadController> logger) 
        { 
            _blob = blob; 
            _env = env;
            _logger = logger;
        }

        [Authorize]
        [HttpGet("test")]
        public IActionResult TestAuth()
        {
            return Ok(new { 
                authenticated = User?.Identity?.IsAuthenticated ?? false,
                userName = User?.Identity?.Name,
                userId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            });
        }

        [Authorize]
        [HttpPost("image")]
        [RequestSizeLimit(25_000_000)]
        public async Task<IActionResult> UploadImage([FromForm] IFormFile file)
        {
            try
            {
                _logger.LogInformation("Upload image request received. User: {User}, FileName: {FileName}, Size: {Size}", 
                    User?.Identity?.Name ?? "Anonymous", file?.FileName, file?.Length);
                
                if (file == null || file.Length == 0) 
                {
                    _logger.LogWarning("No file provided in upload request");
                    return BadRequest(new { error = "Không có file được chọn" });
                }
                
                // Validate file size (max 25MB)
                if (file.Length > 25_000_000)
                {
                    _logger.LogWarning("File too large: {Size} bytes", file.Length);
                    return BadRequest(new { error = "File ảnh quá lớn (tối đa 25MB)" });
                }
                
                var allowed = new[] { "image/jpeg", "image/png", "image/webp", "image/jpg" };
                var contentType = file.ContentType?.ToLowerInvariant() ?? string.Empty;
                var extension = Path.GetExtension(file.FileName ?? string.Empty).ToLowerInvariant();
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                
                if (!allowed.Contains(contentType) && !allowedExtensions.Contains(extension))
                {
                    _logger.LogWarning("Invalid file type: ContentType={ContentType}, Extension={Extension}", contentType, extension);
                    return BadRequest(new { error = $"Định dạng không được hỗ trợ. Chỉ chấp nhận JPG, PNG, WebP. File của bạn: {contentType}" });
                }
                
                _logger.LogInformation("Uploading file to blob storage. ContentType: {ContentType}, Extension: {Extension}", contentType, extension);
                
                await using var s = file.OpenReadStream();
                var url = await _blob.UploadAsync(s, file.ContentType ?? "image/jpeg", file.FileName);
                
                _logger.LogInformation("File uploaded successfully. URL: {Url}", url);
                return Ok(new { url, message = "Tải lên thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image file");
                return StatusCode(500, new { error = $"Lỗi khi tải lên: {ex.Message}", details = ex.ToString() });
            }
        }

        [Authorize]
        [HttpPost("video")]
        [RequestSizeLimit(2_147_483_648)] // 2GB
        [RequestFormLimits(MultipartBodyLengthLimit = MaxVideoFileSize, ValueLengthLimit = int.MaxValue, MultipartHeadersLengthLimit = int.MaxValue)]
        public async Task<IActionResult> UploadVideo([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0) 
                return BadRequest(new { error = "Không có file được chọn" });
            
            if (file.Length > MaxVideoFileSize) 
                return BadRequest(new { error = "File video quá lớn (tối đa 2GB)" });

            var allowedTypes = new[] { "video/mp4", "video/webm", "video/quicktime", "video/x-msvideo" };
            var allowedExtensions = new[] { ".mp4", ".webm", ".mov", ".qt", ".avi" };
            var contentType = file.ContentType?.ToLowerInvariant() ?? string.Empty;
            var extension = Path.GetExtension(file.FileName ?? string.Empty).ToLowerInvariant();

            if (!allowedTypes.Contains(contentType) && !allowedExtensions.Contains(extension))
            {
                return BadRequest(new { error = "Định dạng video không được hỗ trợ. Chỉ chấp nhận mp4, webm" });
            }

            try
            {
                // Tạo thư mục uploads/videos nếu chưa có
                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "videos");
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }

                // Tạo tên file an toàn
                var safeExtension = string.IsNullOrWhiteSpace(extension) ? ".mp4" : extension;
                var safeName = $"{Guid.NewGuid():N}{safeExtension}".Trim();

                var filePath = Path.Combine(uploadsDir, safeName);

                // Lưu file
                await using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Trả về URL tương đối
                var url = $"/uploads/videos/{safeName}";
                return Ok(new { 
                    url, 
                    fileName = safeName,
                    size = file.Length,
                    message = "Tải lên thành công"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Không thể lưu file: {ex.Message}" });
            }
        }
    }
}


