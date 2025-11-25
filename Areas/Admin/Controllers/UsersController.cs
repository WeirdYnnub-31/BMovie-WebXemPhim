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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAdminRole(string id)
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

            // Không cho phép xóa quyền Admin của chính mình
            var currentUser = await _userManager.GetUserAsync(User);
            if (user.Id == currentUser?.Id)
            {
                TempData["Error"] = "Bạn không thể xóa quyền Admin của chính mình.";
                return RedirectToAction(nameof(Index));
            }

            // Không cho phép xóa quyền Admin của admin mặc định (admin@bmovie.local)
            if (user.Email?.Equals("admin@bmovie.local", StringComparison.OrdinalIgnoreCase) == true)
            {
                TempData["Error"] = "Không thể xóa quyền Admin của tài khoản admin mặc định (admin@bmovie.local).";
                return RedirectToAction(nameof(Index));
            }

            // Kiểm tra xem user có role Admin không
            if (!await _userManager.IsInRoleAsync(user, "Admin"))
            {
                TempData["Error"] = $"Tài khoản {user.Email} không có quyền Admin.";
                return RedirectToAction(nameof(Index));
            }

            // Xóa role Admin
            var result = await _userManager.RemoveFromRoleAsync(user, "Admin");
            if (result.Succeeded)
            {
                TempData["Success"] = $"Đã xóa quyền Admin khỏi tài khoản {user.Email}.";
            }
            else
            {
                TempData["Error"] = "Không thể xóa quyền Admin: " + string.Join("; ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAdminRole(string id)
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

            // Kiểm tra xem đã có admin nào chưa (chỉ cho phép 1 admin)
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            if (admins.Count > 0)
            {
                TempData["Error"] = "Hệ thống chỉ cho phép 1 tài khoản Admin. Vui lòng xóa quyền Admin của tài khoản khác trước.";
                return RedirectToAction(nameof(Index));
            }

            // Kiểm tra xem user đã có role Admin chưa
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                TempData["Error"] = $"Tài khoản {user.Email} đã có quyền Admin.";
                return RedirectToAction(nameof(Index));
            }

            // Thêm role Admin
            var result = await _userManager.AddToRoleAsync(user, "Admin");
            if (result.Succeeded)
            {
                TempData["Success"] = $"Đã thêm quyền Admin cho tài khoản {user.Email}.";
            }
            else
            {
                TempData["Error"] = "Không thể thêm quyền Admin: " + string.Join("; ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CleanupAdminRoles()
        {
            const string mainAdminEmail = "admin@bmovie.local";
            
            // Lấy tất cả user có role Admin
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var removedCount = 0;
            var errors = new List<string>();

            foreach (var admin in admins)
            {
                // Bỏ qua admin chính
                if (admin.Email?.Equals(mainAdminEmail, StringComparison.OrdinalIgnoreCase) == true)
                {
                    continue;
                }

                // Xóa quyền Admin
                var result = await _userManager.RemoveFromRoleAsync(admin, "Admin");
                if (result.Succeeded)
                {
                    removedCount++;
                }
                else
                {
                    errors.Add($"{admin.Email}: {string.Join("; ", result.Errors.Select(e => e.Description))}");
                }
            }

            // Đảm bảo admin chính có role Admin
            var mainAdmin = await _userManager.FindByEmailAsync(mainAdminEmail);
            if (mainAdmin != null && !await _userManager.IsInRoleAsync(mainAdmin, "Admin"))
            {
                await _userManager.AddToRoleAsync(mainAdmin, "Admin");
            }

            if (removedCount > 0)
            {
                TempData["Success"] = $"Đã xóa quyền Admin khỏi {removedCount} tài khoản. Chỉ còn {mainAdminEmail} là Admin.";
            }
            else if (errors.Any())
            {
                TempData["Error"] = "Có lỗi khi xóa quyền Admin: " + string.Join("; ", errors);
            }
            else
            {
                TempData["Success"] = $"Đã đảm bảo chỉ có {mainAdminEmail} là Admin.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}


