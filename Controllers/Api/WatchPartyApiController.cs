using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using webxemphim.Services;

namespace webxemphim.Controllers.Api
{
    [ApiController]
    [Route("api/v2/watchparty")]
    [Authorize]
    public class WatchPartyApiController : ControllerBase
    {
        private readonly WatchPartyService _watchPartyService;
        private readonly ILogger<WatchPartyApiController> _logger;

        public WatchPartyApiController(
            WatchPartyService watchPartyService,
            ILogger<WatchPartyApiController> logger)
        {
            _watchPartyService = watchPartyService;
            _logger = logger;
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateWatchPartyRequest request)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var watchParty = await _watchPartyService.CreateWatchPartyAsync(
                userId,
                request.MovieId,
                request.RoomName ?? "Watch Party",
                request.MaxParticipants ?? 10
            );

            if (watchParty == null)
            {
                return BadRequest(new { error = "Không thể tạo watch party." });
            }

            return Ok(new
            {
                roomId = watchParty.RoomId,
                roomName = watchParty.RoomName,
                movieId = watchParty.MovieId,
                hostId = watchParty.HostId,
                createdAt = watchParty.CreatedAt
            });
        }

        [HttpPost("{roomId}/join")]
        public async Task<IActionResult> Join(string roomId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var success = await _watchPartyService.JoinWatchPartyAsync(roomId, userId);
            if (!success)
            {
                return BadRequest(new { error = "Không thể tham gia watch party." });
            }

            return Ok(new { success = true });
        }

        [HttpPost("{roomId}/leave")]
        public async Task<IActionResult> Leave(string roomId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            await _watchPartyService.LeaveWatchPartyAsync(roomId, userId);
            return Ok(new { success = true });
        }

        [HttpPost("{roomId}/playback")]
        public async Task<IActionResult> UpdatePlayback(string roomId, [FromBody] PlaybackStateRequest request)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var success = await _watchPartyService.UpdatePlaybackStateAsync(
                roomId,
                userId,
                request.CurrentTime,
                request.IsPlaying
            );

            if (!success)
            {
                return BadRequest(new { error = "Chỉ host mới có thể cập nhật playback state." });
            }

            return Ok(new { success = true });
        }

        [HttpPost("{roomId}/message")]
        public async Task<IActionResult> SendMessage(string roomId, [FromBody] SendMessageRequest request)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var success = await _watchPartyService.SendMessageAsync(roomId, userId, request.Message);
            if (!success)
            {
                return BadRequest(new { error = "Không thể gửi tin nhắn." });
            }

            return Ok(new { success = true });
        }

        [HttpGet("{roomId}")]
        public async Task<IActionResult> GetWatchParty(string roomId)
        {
            var watchParty = await _watchPartyService.GetWatchPartyAsync(roomId);
            if (watchParty == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                roomId = watchParty.RoomId,
                roomName = watchParty.RoomName,
                movieId = watchParty.MovieId,
                hostId = watchParty.HostId,
                currentTime = watchParty.CurrentTime,
                isPlaying = watchParty.IsPlaying,
                participants = watchParty.Participants.Where(p => p.IsConnected).Select(p => new
                {
                    userId = p.UserId,
                    userName = p.User?.UserName,
                    joinedAt = p.JoinedAt
                }),
                messageCount = watchParty.Messages.Count
            });
        }

        [HttpGet("{roomId}/messages")]
        public async Task<IActionResult> GetMessages(string roomId, int limit = 50)
        {
            var messages = await _watchPartyService.GetMessagesAsync(roomId, limit);
            return Ok(messages.Select(m => new
            {
                id = m.Id,
                userId = m.UserId,
                userName = m.User?.UserName,
                message = m.Message,
                createdAt = m.CreatedAt
            }));
        }
    }

    public class CreateWatchPartyRequest
    {
        public int MovieId { get; set; }
        public string? RoomName { get; set; }
        public int? MaxParticipants { get; set; }
    }

    public class PlaybackStateRequest
    {
        public double CurrentTime { get; set; }
        public bool IsPlaying { get; set; }
    }

    public class SendMessageRequest
    {
        public string Message { get; set; } = string.Empty;
    }
}

