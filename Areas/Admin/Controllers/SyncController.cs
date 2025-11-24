using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace webxemphim.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class SyncController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}


