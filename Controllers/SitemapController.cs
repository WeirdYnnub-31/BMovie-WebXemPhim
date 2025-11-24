using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using webxemphim.Data;

namespace webxemphim.Controllers
{
    public class SitemapController : Controller
    {
        private readonly ApplicationDbContext _db;
        public SitemapController(ApplicationDbContext db) { _db = db; }

        [HttpGet("sitemap.xml")]
        public async Task<IActionResult> Sitemap()
        {
            var baseUrl = string.Concat(Request.Scheme, "://", Request.Host.ToUriComponent(), Request.PathBase.ToUriComponent());
            var urls = new List<string>
            {
                $"{baseUrl}/", $"{baseUrl}/movies", $"{baseUrl}/search", $"{baseUrl}/contact"
            };
            var movies = await _db.Movies.AsNoTracking().OrderByDescending(m=>m.Id).Take(1000).ToListAsync();
            urls.AddRange(movies.Select(m => $"{baseUrl}/movie/{(m.Slug ?? ("phim-"+m.Id))}"));

            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.Append("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
            foreach (var u in urls.Distinct())
            {
                sb.Append("<url>");
                sb.Append($"<loc>{System.Security.SecurityElement.Escape(u)}</loc>");
                sb.Append("</url>");
            }
            sb.Append("</urlset>");
            return Content(sb.ToString(), "application/xml", Encoding.UTF8);
        }

        [HttpGet("robots.txt")]
        public IActionResult Robots()
        {
            var sb = new StringBuilder();
            sb.AppendLine("User-agent: *");
            sb.AppendLine("Disallow:");
            sb.AppendLine("Sitemap: "+ string.Concat(Request.Scheme, "://", Request.Host.ToUriComponent(), "/sitemap.xml"));
            return Content(sb.ToString(), "text/plain", Encoding.UTF8);
        }
    }
}


