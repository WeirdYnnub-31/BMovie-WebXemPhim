using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webxemphim.Data;
using webxemphim.Models;

namespace webxemphim.Controllers
{
    [Authorize]
    public class InventoryController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        public InventoryController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db; _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user!.Id;
            
            // Load items with Movie data included
            var items = await _db.UserInventoryItems
                .AsNoTracking()
                .Include(x => x.Movie)
                    .ThenInclude(m => m!.MovieGenres)
                        .ThenInclude(mg => mg.Genre)
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
            
            ViewBag.Favorites = items.Where(x => x.Type == InventoryItemType.Favorite && x.Movie != null).ToList();
            ViewBag.Watched = items.Where(x => x.Type == InventoryItemType.Watched && x.Movie != null).ToList();
            ViewBag.Rewards = items.Where(x => x.Type == InventoryItemType.Voucher || x.Type == InventoryItemType.Badge).ToList();
            
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFavorite(int movieId)
        {
            var user = await _userManager.GetUserAsync(User);
            var exists = await _db.UserInventoryItems.AnyAsync(x=>x.UserId==user!.Id && x.MovieId==movieId && x.Type==InventoryItemType.Favorite);
            if (!exists)
            {
                _db.UserInventoryItems.Add(new UserInventoryItem { UserId = user!.Id, MovieId = movieId, Type = InventoryItemType.Favorite });
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFavorite(int movieId)
        {
            var user = await _userManager.GetUserAsync(User);
            var item = await _db.UserInventoryItems.FirstOrDefaultAsync(x=>x.UserId==user!.Id && x.MovieId==movieId && x.Type==InventoryItemType.Favorite);
            if (item!=null)
            {
                _db.UserInventoryItems.Remove(item);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddWatched(int movieId)
        {
            var user = await _userManager.GetUserAsync(User);
            var exists = await _db.UserInventoryItems.AnyAsync(x=>x.UserId==user!.Id && x.MovieId==movieId && x.Type==InventoryItemType.Watched);
            if (!exists)
            {
                _db.UserInventoryItems.Add(new UserInventoryItem { UserId = user!.Id, MovieId = movieId, Type = InventoryItemType.Watched });
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveWatched(int movieId)
        {
            var user = await _userManager.GetUserAsync(User);
            var item = await _db.UserInventoryItems.FirstOrDefaultAsync(x=>x.UserId==user!.Id && x.MovieId==movieId && x.Type==InventoryItemType.Watched);
            if (item!=null)
            {
                _db.UserInventoryItems.Remove(item);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}


