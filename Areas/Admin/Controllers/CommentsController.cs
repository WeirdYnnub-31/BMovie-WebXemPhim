using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webxemphim.Data;

namespace webxemphim.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CommentsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public CommentsController(ApplicationDbContext db) { _db = db; }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 30)
        {
            if (page < 1) page = 1; if (pageSize <= 0 || pageSize > 200) pageSize = 30;
            var query = _db.Comments.AsNoTracking().OrderByDescending(x=>x.Id);
            var total = await query.CountAsync();
            var items = await query.Skip((page-1)*pageSize).Take(pageSize).ToListAsync();
            ViewBag.Page = page; ViewBag.PageSize = pageSize; ViewBag.Total = total;
            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var c = await _db.Comments.FindAsync(id);
            if (c == null) return NotFound();
            c.IsApproved = true;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var c = await _db.Comments.FindAsync(id);
            if (c == null) return NotFound();
            _db.Comments.Remove(c);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkApprove(int[] commentIds)
        {
            if (commentIds == null || commentIds.Length == 0)
            {
                TempData["Message"] = "Vui lòng chọn ít nhất một bình luận.";
                return RedirectToAction(nameof(Index));
            }

            var comments = await _db.Comments.Where(c => commentIds.Contains(c.Id)).ToListAsync();
            foreach (var comment in comments)
            {
                comment.IsApproved = true;
            }
            
            await _db.SaveChangesAsync();
            TempData["Message"] = $"Đã duyệt {comments.Count} bình luận.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDelete(int[] commentIds)
        {
            if (commentIds == null || commentIds.Length == 0)
            {
                TempData["Message"] = "Vui lòng chọn ít nhất một bình luận.";
                return RedirectToAction(nameof(Index));
            }

            var comments = await _db.Comments.Where(c => commentIds.Contains(c.Id)).ToListAsync();
            _db.Comments.RemoveRange(comments);
            
            await _db.SaveChangesAsync();
            TempData["Message"] = $"Đã xóa {comments.Count} bình luận.";
            return RedirectToAction(nameof(Index));
        }
    }
}


