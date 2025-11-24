using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace webxemphim.Controllers
{
    [ApiController]
    [Route("img")] 
    public class ImageProxyController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public ImageProxyController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("resize")] 
        public async Task<IActionResult> Resize([FromQuery] string url, [FromQuery] int w = 300, [FromQuery] int h = 0, [FromQuery] int q = 80)
        {
            if (string.IsNullOrWhiteSpace(url)) return BadRequest();
            try
            {
                var client = _httpClientFactory.CreateClient();
                using var resp = await client.GetAsync(url);
                if (!resp.IsSuccessStatusCode) return StatusCode((int)resp.StatusCode);
                await using var stream = await resp.Content.ReadAsStreamAsync();
                using var image = await Image.LoadAsync(stream);
                var size = h > 0 ? new Size(w, h) : new Size(w, 0);
                image.Mutate(x => x.Resize(new ResizeOptions { Size = size, Mode = h > 0 ? ResizeMode.Crop : ResizeMode.Max }));
                var encoder = new JpegEncoder { Quality = Math.Clamp(q, 40, 95) };
                await using var ms = new MemoryStream();
                await image.SaveAsJpegAsync(ms, encoder);
                ms.Position = 0;
                return File(ms.ToArray(), "image/jpeg");
            }
            catch
            {
                return NotFound();
            }
        }
    }
}


