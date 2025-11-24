using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace webxemphim.Pages
{
    public class AboutModel : PageModel
    {
        public void OnGet()
        {
            ViewData["Title"] = "Giới thiệu";
            ViewData["Description"] = "Giới thiệu bmovie – nền tảng xem phim hiện đại, nhanh, thân thiện trên mọi thiết bị.";
        }
    }
}

