using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webxemphim.Data;

namespace webxemphim.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DatabaseController : Controller
    {
        private readonly ApplicationDbContext _db;

        public DatabaseController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpPost]
        public async Task<IActionResult> FixViewHitsColumns()
        {
            try
            {
                // Thêm các cột còn thiếu vào ViewHits
                var sql = @"
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ViewHits]') AND name = 'WatchProgress')
                    BEGIN
                        ALTER TABLE [dbo].[ViewHits] ADD [WatchProgress] [float] NULL;
                    END

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ViewHits]') AND name = 'Duration')
                    BEGIN
                        ALTER TABLE [dbo].[ViewHits] ADD [Duration] [float] NULL;
                    END

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ViewHits]') AND name = 'EpisodeNumber')
                    BEGIN
                        ALTER TABLE [dbo].[ViewHits] ADD [EpisodeNumber] [int] NULL;
                    END

                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ViewHits_UserId_MovieId_EpisodeNumber' AND object_id = OBJECT_ID(N'[dbo].[ViewHits]'))
                    BEGIN
                        CREATE INDEX [IX_ViewHits_UserId_MovieId_EpisodeNumber] 
                        ON [dbo].[ViewHits] ([UserId], [MovieId], [EpisodeNumber]);
                    END
                ";

                await _db.Database.ExecuteSqlRawAsync(sql);
                
                TempData["Success"] = "Đã thêm các cột WatchProgress, Duration, và EpisodeNumber vào bảng ViewHits thành công!";
                return RedirectToAction("Index", "Movies");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
                return RedirectToAction("Index", "Movies");
            }
        }

        [HttpGet]
        public IActionResult MigrationStatus()
        {
            try
            {
                var sql = @"
                    SELECT 
                        CASE WHEN EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ViewHits]') AND name = 'WatchProgress') THEN 1 ELSE 0 END as HasWatchProgress,
                        CASE WHEN EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ViewHits]') AND name = 'Duration') THEN 1 ELSE 0 END as HasDuration,
                        CASE WHEN EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ViewHits]') AND name = 'EpisodeNumber') THEN 1 ELSE 0 END as HasEpisodeNumber
                ";

                var result = _db.Database.SqlQueryRaw<MigrationStatusResult>(sql).FirstOrDefault();
                
                return Json(new
                {
                    success = true,
                    hasWatchProgress = result?.HasWatchProgress == 1,
                    hasDuration = result?.HasDuration == 1,
                    hasEpisodeNumber = result?.HasEpisodeNumber == 1,
                    allColumnsExist = result?.HasWatchProgress == 1 && result?.HasDuration == 1 && result?.HasEpisodeNumber == 1
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        private class MigrationStatusResult
        {
            public int HasWatchProgress { get; set; }
            public int HasDuration { get; set; }
            public int HasEpisodeNumber { get; set; }
        }
    }
}

