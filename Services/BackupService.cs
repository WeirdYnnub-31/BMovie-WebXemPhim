using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using webxemphim.Data;

namespace webxemphim.Services
{
    /// <summary>
    /// Service để backup database và dữ liệu quan trọng
    /// </summary>
    public class BackupService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<BackupService> _logger;
        private readonly IWebHostEnvironment _env;

        public BackupService(
            ApplicationDbContext db,
            ILogger<BackupService> logger,
            IWebHostEnvironment env)
        {
            _db = db;
            _logger = logger;
            _env = env;
        }

        /// <summary>
        /// Backup dữ liệu movies, genres, và các bảng quan trọng
        /// </summary>
        public async Task<string> BackupMoviesDataAsync()
        {
            try
            {
                var backupData = new
                {
                    BackupDate = DateTime.UtcNow,
                    Movies = await _db.Movies
                        .Include(m => m.MovieGenres)
                        .ThenInclude(mg => mg.Genre)
                        .Select(m => new
                        {
                            m.Id,
                            m.Title,
                            m.Slug,
                            m.PosterUrl,
                            m.TrailerUrl,
                            m.Imdb,
                            m.AgeRating,
                            m.DurationMinutes,
                            m.Year,
                            m.Country,
                            m.Description,
                            m.IsSeries,
                            m.ViewCount,
                            m.AverageRating,
                            m.TotalRatings,
                            m.Director,
                            m.Cast,
                            m.ReleaseDate,
                            m.TMDbId,
                            Genres = m.MovieGenres.Select(mg => mg.Genre.Name).ToList()
                        })
                        .ToListAsync(),
                    Genres = await _db.Genres
                        .Select(g => new { g.Id, g.Name, g.Slug })
                        .ToListAsync(),
                    TotalMovies = await _db.Movies.CountAsync(),
                    TotalGenres = await _db.Genres.CountAsync()
                };

                var json = JsonSerializer.Serialize(backupData, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                var backupDir = Path.Combine(_env.ContentRootPath, "Backups");
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }

                var fileName = $"movies_backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
                var filePath = Path.Combine(backupDir, fileName);

                await File.WriteAllTextAsync(filePath, json);

                _logger.LogInformation("Backup created: {FilePath}", filePath);
                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating backup");
                throw;
            }
        }

        /// <summary>
        /// Backup audit logs
        /// </summary>
        public async Task<string> BackupAuditLogsAsync(DateTime? from = null, DateTime? to = null)
        {
            try
            {
                var query = _db.AuditLogs.AsQueryable();
                
                if (from.HasValue)
                    query = query.Where(l => l.CreatedAt >= from.Value);
                
                if (to.HasValue)
                    query = query.Where(l => l.CreatedAt <= to.Value);

                var logs = await query
                    .OrderByDescending(l => l.CreatedAt)
                    .Select(l => new
                    {
                        l.Id,
                        l.Action,
                        l.UserId,
                        l.EntityType,
                        l.EntityId,
                        l.Details,
                        l.IpAddress,
                        l.CreatedAt
                    })
                    .ToListAsync();

                var backupData = new
                {
                    BackupDate = DateTime.UtcNow,
                    FromDate = from,
                    ToDate = to,
                    TotalLogs = logs.Count,
                    Logs = logs
                };

                var json = JsonSerializer.Serialize(backupData, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                var backupDir = Path.Combine(_env.ContentRootPath, "Backups");
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }

                var fileName = $"audit_logs_backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
                var filePath = Path.Combine(backupDir, fileName);

                await File.WriteAllTextAsync(filePath, json);

                _logger.LogInformation("Audit logs backup created: {FilePath}", filePath);
                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating audit logs backup");
                throw;
            }
        }

        /// <summary>
        /// Lấy danh sách các backup files
        /// </summary>
        public List<string> GetBackupFiles()
        {
            try
            {
                var backupDir = Path.Combine(_env.ContentRootPath, "Backups");
                if (!Directory.Exists(backupDir))
                {
                    return new List<string>();
                }

                return Directory.GetFiles(backupDir, "*.json")
                    .OrderByDescending(f => new FileInfo(f).CreationTime)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting backup files");
                return new List<string>();
            }
        }

        /// <summary>
        /// Xóa backup file cũ (giữ lại N file mới nhất)
        /// </summary>
        public void CleanupOldBackups(int keepCount = 10)
        {
            try
            {
                var backupDir = Path.Combine(_env.ContentRootPath, "Backups");
                if (!Directory.Exists(backupDir))
                {
                    return;
                }

                var files = Directory.GetFiles(backupDir, "*.json")
                    .OrderByDescending(f => new FileInfo(f).CreationTime)
                    .Skip(keepCount)
                    .ToList();

                foreach (var file in files)
                {
                    File.Delete(file);
                    _logger.LogInformation("Deleted old backup: {File}", file);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old backups");
            }
        }
    }
}

