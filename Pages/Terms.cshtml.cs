using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace webxemphim.Pages
{
    public class TermsModel : PageModel
    {
        public void OnGet()
        {
            ViewData["Title"] = "Điều khoản sử dụng";
            ViewData["Description"] = "Điều khoản sử dụng bmovie – quy định khi truy cập và sử dụng dịch vụ.";
        }
    }
}

