using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace webxemphim.Pages
{
    public class PrivacyModel : PageModel
    {
        public void OnGet()
        {
            ViewData["Title"] = "Chính sách bảo mật";
            ViewData["Description"] = "Chính sách bảo mật của bmovie – cách chúng tôi thu thập, sử dụng và bảo vệ dữ liệu.";
        }
    }
}

