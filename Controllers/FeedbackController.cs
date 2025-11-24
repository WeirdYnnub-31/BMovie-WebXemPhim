using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webxemphim.Data;
using webxemphim.Models;
using System.Security.Claims;

namespace webxemphim.Controllers
{
    public class FeedbackController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<FeedbackController> _logger;

        public FeedbackController(ApplicationDbContext db, ILogger<FeedbackController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [Authorize]
        [HttpGet]
        public IActionResult Create(int? movieId = null)
        {
            ViewBag.MovieId = movieId;
            if (movieId.HasValue)
            {
                var movie = _db.Movies.Find(movieId.Value);
                ViewBag.MovieTitle = movie?.Title;
            }
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Feedback feedback)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            feedback.UserId = userId;
            feedback.Status = FeedbackStatus.Pending;
            feedback.CreatedAt = DateTime.UtcNow;
            feedback.Priority = DeterminePriority(feedback.Type);

            if (ModelState.IsValid)
            {
                _db.Feedbacks.Add(feedback);
                await _db.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Cảm ơn bạn đã gửi phản hồi! Chúng tôi sẽ xem xét và phản hồi sớm nhất có thể.";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.MovieId = feedback.MovieId;
            if (feedback.MovieId.HasValue)
            {
                var movie = await _db.Movies.FindAsync(feedback.MovieId.Value);
                ViewBag.MovieTitle = movie?.Title;
            }
            return View(feedback);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MyFeedbacks()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var feedbacks = await _db.Feedbacks
                .Include(f => f.Movie)
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            return View(feedbacks);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var feedback = await _db.Feedbacks
                .Include(f => f.Movie)
                .Include(f => f.Admin)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (feedback == null)
            {
                return NotFound();
            }

            // Chỉ cho phép xem feedback của chính mình hoặc admin
            if (feedback.UserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            return View(feedback);
        }

        private int DeterminePriority(FeedbackType type)
        {
            return type switch
            {
                FeedbackType.MovieError => 4, // Critical
                FeedbackType.AudioError => 4, // Critical
                FeedbackType.VideoQuality => 3, // High
                FeedbackType.SubtitleError => 3, // High
                FeedbackType.BugReport => 3, // High
                FeedbackType.FeatureRequest => 2, // Medium
                FeedbackType.Suggestion => 1, // Low
                _ => 1 // Low
            };
        }
    }
}

