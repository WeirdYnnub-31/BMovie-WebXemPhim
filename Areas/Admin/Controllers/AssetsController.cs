using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace webxemphim.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AssetsController : Controller
    {
        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }
    }
}


