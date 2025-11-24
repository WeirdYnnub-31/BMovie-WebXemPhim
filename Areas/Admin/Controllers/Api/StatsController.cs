using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webxemphim.Data;

namespace webxemphim.Areas.Admin.Controllers.Api
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    [Route("admin/api/[controller]/[action]")]
    [ApiController]
    public class StatsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public StatsController(ApplicationDbContext db) { _db = db; }

        [HttpGet]
        public async Task<IActionResult> GetViewHits(int days = 7)
        {
            if (days <= 0 || days > 90) days = 7;
            var end = DateTime.UtcNow.Date.AddDays(1); // exclusive
            var start = end.AddDays(-days);
            var buckets = await _db.ViewHits.AsNoTracking()
                .Where(v => v.ViewedAt >= start && v.ViewedAt < end)
                .GroupBy(v => v.ViewedAt.Date)
                .Select(g => new { Day = g.Key, Count = g.LongCount() })
                .ToListAsync();
            var labels = new List<string>();
            var data = new List<long>();
            for (int i = days - 1; i >= 0; i--)
            {
                var d = end.AddDays(-1 - i).Date;
                labels.Add(d.ToString("dd/MM"));
                var found = buckets.FirstOrDefault(x => x.Day == d);
                data.Add(found?.Count ?? 0);
            }
            return Ok(new { labels, data });
        }
    }
}


