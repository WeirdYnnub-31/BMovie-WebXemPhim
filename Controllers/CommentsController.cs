using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using webxemphim.Data;
using webxemphim.Models;
using webxemphim.Services;

namespace webxemphim.Controllers
{
    [Authorize]
    public class CommentsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly webxemphim.Services.AI.ModerationService _moderation;
        private readonly CoinService _coinService;
        private readonly AchievementService _achievementService;
        private readonly NotificationService _notificationService;
        private readonly IServiceProvider _serviceProvider;
        
        public CommentsController(
            ApplicationDbContext db, 
            UserManager<ApplicationUser> userManager, 
            webxemphim.Services.AI.ModerationService moderation,
            CoinService coinService,
            AchievementService achievementService,
            NotificationService notificationService,
            IServiceProvider serviceProvider)
        {
            _db = db; 
            _userManager = userManager; 
            _moderation = moderation;
            _coinService = coinService;
            _achievementService = achievementService;
            _notificationService = notificationService;
            _serviceProvider = serviceProvider;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int movieId, string content, int? parentCommentId = null, string? returnUrl = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (string.IsNullOrWhiteSpace(content))
            {
                // Try to get movie slug to redirect
                var movie = await _db.Movies.AsNoTracking().FirstOrDefaultAsync(m => m.Id == movieId);
                if (movie != null && !string.IsNullOrWhiteSpace(movie.Slug))
                {
                    return RedirectToAction("Watch", "Movies", new { slug = movie.Slug });
                }
                return RedirectToAction("Index", "Home");
            }

            var allowed = await _moderation.IsAllowedAsync(content, HttpContext.RequestAborted);
            var isApproved = allowed && _moderation.IsConfigured; // only auto-approve when AI configured and content allowed
            
            var comment = new Comment 
            { 
                MovieId = movieId, 
                UserId = user!.Id, 
                Content = content, 
                IsApproved = isApproved,
                ParentCommentId = parentCommentId
            };
            
            _db.Comments.Add(comment);
            await _db.SaveChangesAsync();
            
            // Thưởng coin và cập nhật achievement nếu comment được approve
            if (isApproved)
            {
                var movie = await _db.Movies.FindAsync(movieId);
                if (movie != null)
                {
                    // Thưởng coin khi bình luận
                    await _coinService.RewardCommentAsync(user.Id, movieId);
                    
                    // Cập nhật achievement
                    await _achievementService.CheckAndUpdateAchievementsAsync(user.Id, AchievementType.CommentsWritten);
                }
            }
            
            // Gửi thông báo nếu là reply comment
            if (parentCommentId.HasValue)
            {
                var parentComment = await _db.Comments
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.Id == parentCommentId.Value);
                
                if (parentComment != null && parentComment.UserId != user.Id)
                {
                    await _notificationService.NotifyCommentReplyAsync(
                        parentComment.UserId,
                        comment.Id,
                        user.UserName ?? user.Email ?? "Người dùng",
                        movieId
                    );
                }
            }
            
            TempData["CommentNotice"] = isApproved ? "Bình luận đã được đăng." : "Bình luận đã gửi, chờ duyệt.";

            // Redirect to Watch page if returnUrl is provided, otherwise try to get movie slug
            if (!string.IsNullOrWhiteSpace(returnUrl))
            {
                return Redirect(returnUrl);
            }

            var movieSlugForRedirect = await _db.Movies.AsNoTracking()
                .Where(m => m.Id == movieId)
                .Select(m => m.Slug)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrWhiteSpace(movieSlugForRedirect))
            {
                return RedirectToAction("Watch", "Movies", new { slug = movieSlugForRedirect });
            }

            // Fallback to Detail page
            return RedirectToAction("Detail", "Movies", new { slug = "phim-demo-" + movieId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Like(int commentId)
        {
            var comment = await _db.Comments.FindAsync(commentId);
            if (comment != null)
            {
                comment.Likes++;
                await _db.SaveChangesAsync();
            }
            return Json(new { likes = comment?.Likes ?? 0 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Dislike(int commentId)
        {
            var comment = await _db.Comments.FindAsync(commentId);
            if (comment != null)
            {
                comment.Dislikes++;
                await _db.SaveChangesAsync();
            }
            return Json(new { dislikes = comment?.Dislikes ?? 0 });
        }
    }
}


