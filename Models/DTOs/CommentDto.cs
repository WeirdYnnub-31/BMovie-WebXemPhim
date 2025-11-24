namespace webxemphim.Models.DTOs
{
    public class CommentDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public string? UserAvatar { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? ParentCommentId { get; set; }
        public List<CommentDto> Replies { get; set; } = new();
        public int LikeCount { get; set; }
        public int DislikeCount { get; set; }
    }

    public class CreateCommentDto
    {
        public int MovieId { get; set; }
        public string Content { get; set; } = string.Empty;
        public int? ParentCommentId { get; set; }
    }

    public class UpdateCommentDto
    {
        public string Content { get; set; } = string.Empty;
    }
}

