using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using webxemphim.Data;

namespace webxemphim.ViewComponents
{
    public class UserMenuViewComponent : ViewComponent
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserMenuViewComponent(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (HttpContext.User?.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(HttpContext.User);
                var name = user?.Email ?? HttpContext.User.Identity?.Name ?? "User";
                var avatarClaim = HttpContext.User.FindFirst("avatar")?.Value;
                
                ViewBag.UserEmail = name;
                ViewBag.AvatarClaim = avatarClaim;
                ViewBag.Initial = string.IsNullOrWhiteSpace(name) ? "U" : name.Trim()[0].ToString().ToUpper();
            }
            return View();
        }
    }
}

