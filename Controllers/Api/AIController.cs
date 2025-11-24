using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using webxemphim.Services.AI;

namespace webxemphim.Controllers.Api
{
    [ApiController]
    [Route("api/ai")]
    public class AIController : ControllerBase
    {
        private readonly OpenAIService _ai;
        private readonly ILogger<AIController> _logger;
        public AIController(OpenAIService ai, ILogger<AIController> logger)
        {
            _ai = ai; _logger = logger;
        }

        // [HttpPost("chat")]
        // public async Task<IActionResult> Chat([FromBody] ChatRequest req, CancellationToken ct)
        // {
        //     // Disabled to avoid OpenAI costs
        //     return StatusCode(501, new { error = "AI Chat feature is disabled." });
        //     // if (!_ai.IsConfigured) return StatusCode(503, new { error = "AI not configured" });
        //     // var msgs = req.Messages?.Select(m => (m.Role ?? "user", m.Content ?? string.Empty)) ?? new[] { ("user", req.Prompt ?? string.Empty) };
        //     // var text = await _ai.ChatAsync(msgs, ct);
        //     // return Ok(new { text });
        // }

        // [Authorize]
        // [HttpPost("recommend")]
        // public async Task<IActionResult> Recommend([FromBody] RecommendRequest req, CancellationToken ct)
        // {
        //     // Disabled to avoid OpenAI costs
        //     return StatusCode(501, new { error = "AI Recommend feature is disabled." });
        //     // if (!_ai.IsConfigured) return StatusCode(503, new { error = "AI not configured" });
        //     // // Placeholder: integrate with your DB + embeddings later
        //     // var user = User.Identity?.Name ?? "you";
        //     // var prompt = $"Đề xuất 10 phim theo sở thích của {user}. Gợi ý: {string.Join(", ", req?.LikedTitles ?? new List<string>())}";
        //     // var text = await _ai.ChatAsync(new[] { ("system","Bạn là chuyên gia gợi ý phim."), ("user", prompt) }, ct);
        //     // return Ok(new { text });
        // }

        // [HttpPost("summarize")]
        // public async Task<IActionResult> Summarize([FromBody] SummarizeRequest req, CancellationToken ct)
        // {
        //     // Disabled to avoid OpenAI costs
        //     return StatusCode(501, new { error = "AI Summarize feature is disabled." });
        //     // if (!_ai.IsConfigured) return StatusCode(503, new { error = "AI not configured" });
        //     // var prompt = $"Tóm tắt nội dung phim (150 chữ, tiếng Việt, hấp dẫn):\n\n{req?.Text}";
        //     // var text = await _ai.ChatAsync(new[] { ("user", prompt) }, ct);
        //     // return Ok(new { text });
        // }
    }

    public class ChatRequest { public string? Prompt { get; set; } public List<ChatMessage>? Messages { get; set; } }
    public class ChatMessage { public string? Role { get; set; } public string? Content { get; set; } }
    public class SummarizeRequest { public string? Text { get; set; } }
    public class RecommendRequest { public List<string>? LikedTitles { get; set; } }
}


