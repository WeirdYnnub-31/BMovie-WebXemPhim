using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using webxemphim.Data;

namespace webxemphim.Controllers
{
    /// <summary>
    /// Controller để khôi phục tài khoản admin nếu bị xóa
    /// Chỉ chạy được khi chưa có admin nào trong hệ thống
    /// </summary>
    public class AdminRecoveryController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminRecoveryController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAdmin(string? email = null, string? password = null)
        {
            // Kiểm tra xem đã có admin nào chưa
            var adminRole = await _roleManager.FindByNameAsync("Admin");
            if (adminRole != null)
            {
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                if (admins.Count > 0)
                {
                    ViewBag.Error = "Đã có admin trong hệ thống. Không thể tạo lại admin từ đây. Vui lòng đăng nhập với tài khoản admin hiện có.";
                    return View("Index");
                }
            }

            // Tạo role Admin nếu chưa có
            if (adminRole == null)
            {
                adminRole = new IdentityRole("Admin");
                await _roleManager.CreateAsync(adminRole);
            }

            // Sử dụng thông tin mặc định hoặc thông tin được cung cấp
            var adminEmail = email ?? "admin@bmovie.local";
            var adminPassword = password ?? "Admin@12345";

            // Kiểm tra xem email đã tồn tại chưa
            var existingUser = await _userManager.FindByEmailAsync(adminEmail);
            if (existingUser != null)
            {
                // Nếu user đã tồn tại nhưng chưa có role Admin, thêm role
                if (!await _userManager.IsInRoleAsync(existingUser, "Admin"))
                {
                    await _userManager.AddToRoleAsync(existingUser, "Admin");
                    ViewBag.Success = $"Đã thêm quyền Admin cho tài khoản {adminEmail}.";
                    return View("Index");
                }
                else
                {
                    ViewBag.Error = $"Tài khoản {adminEmail} đã tồn tại và đã có quyền Admin.";
                    return View("Index");
                }
            }

            // Tạo user mới
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
            {
                // Thêm role Admin
                await _userManager.AddToRoleAsync(admin, "Admin");
                ViewBag.Success = $"Đã tạo lại tài khoản admin thành công!<br/>Email: {adminEmail}<br/>Mật khẩu: {adminPassword}";
            }
            else
            {
                ViewBag.Error = "Không thể tạo tài khoản admin: " + string.Join("; ", result.Errors.Select(e => e.Description));
            }

            return View("Index");
        }
    }
}

