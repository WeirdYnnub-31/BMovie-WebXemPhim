using webxemphim.Data;

namespace webxemphim.Models
{
    public class WatchParty
    {
        public int Id { get; set; }
        public string RoomId { get; set; } = string.Empty; // Unique room identifier
        public string HostId { get; set; } = string.Empty; // User ID của host
        public int MovieId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public double CurrentTime { get; set; } = 0; // Thời điểm phát hiện tại (seconds)
        public bool IsPlaying { get; set; } = false; // Đang phát hay đã pause
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? EndedAt { get; set; }
        public int MaxParticipants { get; set; } = 10;

        // Navigation properties
        public ApplicationUser? Host { get; set; }
        public Movie? Movie { get; set; }
        public List<WatchPartyParticipant> Participants { get; set; } = new();
        public List<WatchPartyMessage> Messages { get; set; } = new();
    }

    public class WatchPartyParticipant
    {
        public int Id { get; set; }
        public int WatchPartyId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public bool IsConnected { get; set; } = true;

        // Navigation properties
        public WatchParty? WatchParty { get; set; }
        public ApplicationUser? User { get; set; }
    }

    public class WatchPartyMessage
    {
        public int Id { get; set; }
        public int WatchPartyId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public WatchParty? WatchParty { get; set; }
        public ApplicationUser? User { get; set; }
    }
}

