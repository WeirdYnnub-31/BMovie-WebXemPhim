namespace webxemphim.Services.AI
{
    public class ModerationService
    {
        private readonly OpenAIService _ai;
        public ModerationService(OpenAIService ai) { _ai = ai; }
        public bool IsConfigured => _ai.IsConfigured;

        public async Task<bool> IsAllowedAsync(string content, CancellationToken ct)
        {
            if (!_ai.IsConfigured) return true;
            var sys = ("system", "Bạn là bộ lọc nội dung. Trả lời 'ALLOW' nếu bình luận an toàn, 'BLOCK' nếu chứa nội dung tục tĩu, kích động thù hằn, spam.");
            var user = ("user", content ?? "");
            var res = await _ai.ChatAsync(new[] { sys, user }, ct);
            return res.Trim().ToUpperInvariant().Contains("ALLOW");
        }
    }
}


