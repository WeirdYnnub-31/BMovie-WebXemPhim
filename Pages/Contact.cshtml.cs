using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace webxemphim.Pages
{
    public class ContactModel : PageModel
    {
        public void OnGet()
        {
            ViewData["Title"] = "Liên hệ";
            ViewData["Description"] = "Liên hệ đội ngũ bmovie – góp ý, báo lỗi, đề xuất phim.";
        }
    }
}

