using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webxemphim.Data;

namespace webxemphim.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        
        public UsersController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string? search = null)
        {
            if (page < 1) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 20;
            
            var query = _db.Users.AsNoTracking().AsQueryable();
            
            // Tìm kiếm theo email
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u => u.Email != null && u.Email.Contains(search));
            }
            
            query = query.OrderBy(u => u.Email);
            
            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Total = total;
            ViewBag.Search = search;
            
            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Disable(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "ID người dùng không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy người dùng.";
                return RedirectToAction(nameof(Index));
            }

            // Không cho phép disable chính mình
            var currentUser = await _userManager.GetUserAsync(User);
            if (user.Id == currentUser?.Id)
            {
                TempData["Error"] = "Bạn không thể dừng hoạt động tài khoản của chính mình.";
                return RedirectToAction(nameof(Index));
            }

            // Disable bằng cách lock account đến năm 2099
            var result = await _userManager.SetLockoutEnabledAsync(user, true);
            if (result.Succeeded)
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
                TempData["Success"] = $"Đã dừng hoạt động tài khoản {user.Email}.";
            }
            else
            {
                TempData["Error"] = "Không thể dừng hoạt động tài khoản: " + string.Join("; ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enable(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "ID người dùng không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy người dùng.";
                return RedirectToAction(nameof(Index));
            }

            // Enable bằng cách unlock account
            var result = await _userManager.SetLockoutEndDateAsync(user, null);
            if (result.Succeeded)
            {
                await _userManager.SetLockoutEnabledAsync(user, false);
                await _userManager.ResetAccessFailedCountAsync(user);
                TempData["Success"] = $"Đã kích hoạt lại tài khoản {user.Email}.";
            }
            else
            {
                TempData["Error"] = "Không thể kích hoạt tài khoản: " + string.Join("; ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "ID người dùng không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy người dùng.";
                return RedirectToAction(nameof(Index));
            }

            // Không cho phép xóa chính mình
            var currentUser = await _userManager.GetUserAsync(User);
            if (user.Id == currentUser?.Id)
            {
                TempData["Error"] = "Bạn không thể xóa tài khoản của chính mình.";
                return RedirectToAction(nameof(Index));
            }

            var userEmail = user.Email;
            
            // Xóa user (sẽ cascade delete các bảng liên quan)
            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = $"Đã xóa tài khoản {userEmail}.";
            }
            else
            {
                TempData["Error"] = "Không thể xóa tài khoản: " + string.Join("; ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Index));
        }
    }
}


