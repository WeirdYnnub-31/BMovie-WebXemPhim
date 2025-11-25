using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using webxemphim.Data;

namespace webxemphim.ViewComponents
{
    public class UserMenuViewComponent : ViewComponent
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UserMenuViewComponent> _logger;

        public UserMenuViewComponent(UserManager<ApplicationUser> userManager, ILogger<UserMenuViewComponent> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (HttpContext.User?.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(HttpContext.User);
                // Ưu tiên hiển thị UserName, nếu UserName là email thì lấy phần trước @
                string name;
                if (user != null)
                {
                    // Log để debug (chỉ trong development)
                    _logger.LogDebug("UserMenu - UserId: {UserId}, Email: {Email}, UserName: {UserName}", 
                        user.Id, user.Email, user.UserName);
                    
                    // Kiểm tra nếu là admin user mặc định
                    if (user.Email?.Equals("admin@bmovie.local", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        name = "Admin";
                    }
                    else if (!string.IsNullOrWhiteSpace(user.UserName) && !user.UserName.Contains("@"))
                    {
                        // UserName không phải email, dùng trực tiếp
                        name = user.UserName;
                    }
                    else if (!string.IsNullOrWhiteSpace(user.UserName))
                    {
                        // UserName là email, lấy phần trước @
                        name = user.UserName.Split('@')[0];
                    }
                    else
                    {
                        // Không có UserName, dùng phần trước @ của Email
                        name = user.Email?.Split('@')[0] ?? HttpContext.User.Identity?.Name ?? "User";
                    }
                }
                else
                {
                    name = HttpContext.User.Identity?.Name ?? "User";
                    // Nếu name là email, lấy phần trước @
                    if (name.Contains("@"))
                    {
                        name = name.Split('@')[0];
                    }
                }
                
                // Lấy avatar claim từ HttpContext trước, nếu không có thì lấy từ database
                var avatarClaim = HttpContext.User.FindFirst("avatar")?.Value;
                
                // Nếu không có trong claims, lấy trực tiếp từ database
                if (string.IsNullOrEmpty(avatarClaim) && user != null)
                {
                    var claims = await _userManager.GetClaimsAsync(user);
                    avatarClaim = claims.FirstOrDefault(c => c.Type == "avatar")?.Value;
                }
                
                ViewBag.UserEmail = name;
                ViewBag.AvatarClaim = avatarClaim;
                ViewBag.Initial = string.IsNullOrWhiteSpace(name) ? "U" : name.Trim()[0].ToString().ToUpper();
            }
            return View();
        }
    }
}

