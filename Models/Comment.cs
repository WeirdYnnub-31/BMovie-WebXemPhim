using Microsoft.AspNetCore.Identity;
using webxemphim.Data;

namespace webxemphim.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public int MovieId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsApproved { get; set; }
        public int? ParentCommentId { get; set; } // For nested replies
        public int Likes { get; set; } = 0;
        public int Dislikes { get; set; } = 0;

        // Navigation properties
        public Movie Movie { get; set; } = null!;
        public ApplicationUser? User { get; set; }
        public Comment? ParentComment { get; set; }
        public List<Comment> Replies { get; set; } = new();
    }
}


