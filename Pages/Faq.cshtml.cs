using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace webxemphim.Pages
{
    public class FaqModel : PageModel
    {
        public void OnGet()
        {
            ViewData["Title"] = "Hỏi-Đáp";
            ViewData["Description"] = "Những câu hỏi thường gặp khi sử dụng bmovie – nền tảng xem phim hiện đại.";
        }
    }
}

