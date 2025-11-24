using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using webxemphim.Services;

namespace webxemphim.Controllers.Api
{
    [ApiController]
    [Route("api/v2/coin")]
    [Authorize]
    public class CoinApiController : ControllerBase
    {
        private readonly CoinService _coinService;
        private readonly ILogger<CoinApiController> _logger;

        public CoinApiController(
            CoinService coinService,
            ILogger<CoinApiController> logger)
        {
            _coinService = coinService;
            _logger = logger;
        }

        [HttpGet("balance")]
        public async Task<IActionResult> GetBalance()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var balance = await _coinService.GetBalanceAsync(userId);
            var wallet = await _coinService.GetOrCreateWalletAsync(userId);

            return Ok(new
            {
                balance = balance,
                totalEarned = wallet.TotalEarned,
                totalSpent = wallet.TotalSpent
            });
        }

        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions(int page = 1, int pageSize = 20)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var transactions = await _coinService.GetTransactionHistoryAsync(userId, page, pageSize);
            return Ok(transactions.Select(t => new
            {
                id = t.Id,
                amount = t.Amount,
                type = t.Type.ToString(),
                description = t.Description,
                movieId = t.MovieId,
                createdAt = t.CreatedAt
            }));
        }

        [HttpPost("unlock-movie")]
        public async Task<IActionResult> UnlockMovie([FromBody] UnlockMovieRequest request)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var (success, error) = await _coinService.UnlockMovieAsync(userId, request.MovieId, request.CoinCost);
            if (!success)
            {
                return BadRequest(new { error = error ?? "Không thể mở khóa phim." });
            }

            return Ok(new { success = true, message = "Đã mở khóa phim thành công." });
        }
    }

    public class UnlockMovieRequest
    {
        public int MovieId { get; set; }
        public int CoinCost { get; set; }
    }
}

