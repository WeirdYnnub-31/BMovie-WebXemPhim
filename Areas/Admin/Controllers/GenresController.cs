using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webxemphim.Data;
using webxemphim.Models;

namespace webxemphim.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class GenresController : Controller
    {
        private readonly ApplicationDbContext _db;
        public GenresController(ApplicationDbContext db) { _db = db; }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 20;
            var query = _db.Genres.AsNoTracking().OrderBy(g => g.Name);
            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            ViewBag.Page = page; ViewBag.PageSize = pageSize; ViewBag.Total = total;
            return View(items);
        }

        public IActionResult Create() => View(new Genre());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Genre model)
        {
            if (!ModelState.IsValid) return View(model);
            _db.Genres.Add(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var item = await _db.Genres.FindAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Genre model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);
            _db.Update(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _db.Genres.FindAsync(id);
            if (item == null) return NotFound();
            _db.Genres.Remove(item);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}


