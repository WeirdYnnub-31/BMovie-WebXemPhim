namespace webxemphim.Services
{
    public class CdnService
    {
        private readonly string? _baseUrl;
        public CdnService(IConfiguration configuration)
        {
            _baseUrl = configuration["Cdn:BaseUrl"];
            if (!string.IsNullOrWhiteSpace(_baseUrl)) _baseUrl = _baseUrl!.TrimEnd('/');
        }

        public string Resolve(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return "/favicon.ico";
            if (url.StartsWith("http://") || url.StartsWith("https://")) return url;
            if (string.IsNullOrWhiteSpace(_baseUrl)) return url;
            if (url.StartsWith("/")) return _baseUrl + url;
            return _baseUrl + "/" + url;
        }
    }
}


