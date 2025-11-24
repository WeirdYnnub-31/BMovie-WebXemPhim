using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using webxemphim.Services;

namespace webxemphim.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class BackupController : Controller
    {
        private readonly BackupService _backupService;
        private readonly ILogger<BackupController> _logger;

        public BackupController(BackupService backupService, ILogger<BackupController> logger)
        {
            _backupService = backupService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var backupFiles = _backupService.GetBackupFiles();
            ViewBag.BackupFiles = backupFiles.Select(f => new
            {
                FileName = System.IO.Path.GetFileName(f),
                FilePath = f,
                Created = System.IO.File.GetCreationTime(f),
                Size = new System.IO.FileInfo(f).Length
            }).ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMoviesBackup()
        {
            try
            {
                var filePath = await _backupService.BackupMoviesDataAsync();
                TempData["Success"] = $"Backup đã được tạo thành công: {System.IO.Path.GetFileName(filePath)}";
                _backupService.CleanupOldBackups(10); // Giữ lại 10 file mới nhất
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating movies backup");
                TempData["Error"] = $"Lỗi khi tạo backup: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAuditLogsBackup(DateTime? from, DateTime? to)
        {
            try
            {
                var filePath = await _backupService.BackupAuditLogsAsync(from, to);
                TempData["Success"] = $"Backup audit logs đã được tạo thành công: {System.IO.Path.GetFileName(filePath)}";
                _backupService.CleanupOldBackups(10);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating audit logs backup");
                TempData["Error"] = $"Lỗi khi tạo backup: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult DownloadBackup(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            var fileName = System.IO.Path.GetFileName(filePath);
            return File(fileBytes, "application/json", fileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteBackup(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath) || !System.IO.File.Exists(filePath))
                {
                    TempData["Error"] = "File không tồn tại";
                    return RedirectToAction(nameof(Index));
                }

                System.IO.File.Delete(filePath);
                TempData["Success"] = "File backup đã được xóa";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting backup file");
                TempData["Error"] = $"Lỗi khi xóa file: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

