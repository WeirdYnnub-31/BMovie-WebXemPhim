using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using webxemphim.Data;
using webxemphim.Models;
using webxemphim.Services;
using System.Security.Claims;

namespace webxemphim.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly CoinService _coinService;
        private readonly AchievementService _achievementService;
        private readonly SocialService _socialService;
        private readonly ApiKeyService _apiKeyService;
        private readonly ApplicationDbContext _db;
        
        public ProfileController(
            UserManager<ApplicationUser> userManager, 
            SignInManager<ApplicationUser> signInManager,
            CoinService coinService,
            AchievementService achievementService,
            SocialService socialService,
            ApiKeyService apiKeyService,
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _coinService = coinService;
            _achievementService = achievementService;
            _socialService = socialService;
            _apiKeyService = apiKeyService;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                // User không tồn tại hoặc session hết hạn
                await HttpContext.SignOutAsync();
                return RedirectToAction("Login", "Account");
            }
            
            // Lấy avatar claim mới nhất từ database
            var claims = await _userManager.GetClaimsAsync(user);
            var avatarClaim = claims.FirstOrDefault(c => c.Type == "avatar")?.Value;
            
            ViewBag.CurrentUserEmail = user.Email;
            ViewBag.CurrentUserId = user.Id;
            ViewBag.AvatarUrl = avatarClaim; // Pass avatar URL từ database
            
            // Lấy coin balance
            var coinBalance = await _coinService.GetBalanceAsync(user.Id);
            ViewBag.CoinBalance = coinBalance;
            
            // Lấy achievements
            var achievements = await _achievementService.GetUserAchievementsAsync(user.Id);
            ViewBag.Achievements = achievements;
            ViewBag.UnlockedCount = achievements.Count(a => a.IsUnlocked);
            
            // Lấy transaction history (10 giao dịch gần nhất)
            var transactions = await _coinService.GetTransactionHistoryAsync(user.Id, 1, 10);
            ViewBag.Transactions = transactions;
            
            // Lấy thống kê
            var watchedCount = await _db.UserInventoryItems
                .CountAsync(ui => ui.UserId == user.Id && ui.Type == InventoryItemType.Watched);
            var favoriteCount = await _db.UserInventoryItems
                .CountAsync(ui => ui.UserId == user.Id && ui.Type == InventoryItemType.Favorite);
            var ratingCount = await _db.Ratings.CountAsync(r => r.UserId == user.Id);
            var commentCount = await _db.Comments.CountAsync(c => c.UserId == user.Id && c.IsApproved);
            
            ViewBag.WatchedCount = watchedCount;
            ViewBag.FavoriteCount = favoriteCount;
            ViewBag.RatingCount = ratingCount;
            ViewBag.CommentCount = commentCount;
            
            // Social stats
            var followerCount = await _socialService.GetFollowerCountAsync(user.Id);
            var followingCount = await _socialService.GetFollowingCountAsync(user.Id);
            ViewBag.FollowerCount = followerCount;
            ViewBag.FollowingCount = followingCount;
            
            return View(user);
        }

        [HttpGet("ViewProfile/{userId}")]
        [AllowAnonymous]
        public async Task<IActionResult> ViewProfile(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var currentUserId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            ViewBag.IsOwnProfile = currentUserId == userId;
            ViewBag.IsFollowing = false;
            
            if (!string.IsNullOrEmpty(currentUserId) && currentUserId != userId)
            {
                ViewBag.IsFollowing = await _socialService.IsFollowingAsync(currentUserId, userId);
            }

            // Get user stats
            var watchedCount = await _db.UserInventoryItems
                .CountAsync(ui => ui.UserId == userId && ui.Type == InventoryItemType.Watched);
            var favoriteCount = await _db.UserInventoryItems
                .CountAsync(ui => ui.UserId == userId && ui.Type == InventoryItemType.Favorite);
            var ratingCount = await _db.Ratings.CountAsync(r => r.UserId == userId);
            var commentCount = await _db.Comments.CountAsync(c => c.UserId == userId && c.IsApproved);

            ViewBag.WatchedCount = watchedCount;
            ViewBag.FavoriteCount = favoriteCount;
            ViewBag.RatingCount = ratingCount;
            ViewBag.CommentCount = commentCount;

            // Social stats
            var followerCount = await _socialService.GetFollowerCountAsync(userId);
            var followingCount = await _socialService.GetFollowingCountAsync(userId);
            ViewBag.FollowerCount = followerCount;
            ViewBag.FollowingCount = followingCount;

            // Get recent activity
            var recentComments = await _db.Comments
                .Include(c => c.Movie)
                .Where(c => c.UserId == userId && c.IsApproved)
                .OrderByDescending(c => c.CreatedAt)
                .Take(10)
                .ToListAsync();
            ViewBag.RecentComments = recentComments;

            var recentRatings = await _db.Ratings
                .Include(r => r.Movie)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .Take(10)
                .ToListAsync();
            ViewBag.RecentRatings = recentRatings;

            return View("ViewProfile", user);
        }

        [HttpGet("ApiKeys")]
        public async Task<IActionResult> ApiKeys()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                await HttpContext.SignOutAsync();
                return RedirectToAction("Login", "Account");
            }

            var apiKeys = await _apiKeyService.GetUserApiKeysAsync(user.Id);
            return View(apiKeys);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword != confirmPassword)
            {
                TempData["ProfileMsg"] = "Mật khẩu xác nhận không khớp.";
                return RedirectToAction(nameof(Index));
            }
            var user = await _userManager.GetUserAsync(User);
            var result = await _userManager.ChangePasswordAsync(user!, currentPassword, newPassword);
            TempData["ProfileMsg"] = result.Succeeded ? "Đổi mật khẩu thành công." : string.Join("; ", result.Errors.Select(e=>e.Description));
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAvatar(IFormFile avatar)
        {
            if (avatar == null || avatar.Length == 0)
            {
                TempData["ProfileMsg"] = "Chưa chọn ảnh.";
                return RedirectToAction(nameof(Index));
            }
            
            // Validate file type
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(avatar.ContentType))
            {
                TempData["ProfileMsg"] = "Chỉ chấp nhận file ảnh (JPG, PNG, GIF, WebP).";
                return RedirectToAction(nameof(Index));
            }
            
            // Validate file size (max 5MB)
            if (avatar.Length > 5 * 1024 * 1024)
            {
                TempData["ProfileMsg"] = "Kích thước ảnh không được vượt quá 5MB.";
                return RedirectToAction(nameof(Index));
            }
            
            var user = await _userManager.GetUserAsync(User);
            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");
            Directory.CreateDirectory(uploads);
            
            // Delete old avatar if exists
            var oldClaims = await _userManager.GetClaimsAsync(user!);
            var oldAvatar = oldClaims.FirstOrDefault(c => c.Type == "avatar");
            if (oldAvatar != null)
            {
                var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", oldAvatar.Value.TrimStart('/'));
                if (System.IO.File.Exists(oldPath))
                {
                    System.IO.File.Delete(oldPath);
                }
                await _userManager.RemoveClaimAsync(user!, oldAvatar);
            }
            
            try
            {
                // Tạo tên file unique
                var fileExtension = Path.GetExtension(avatar.FileName) ?? ".jpg";
                var fileName = $"ava_{user!.Id}_{DateTime.UtcNow:yyyyMMddHHmmssfff}{fileExtension}";
                var path = Path.Combine(uploads, fileName);
                
                // Lưu file
                using (var stream = System.IO.File.Create(path))
                {
                    await avatar.CopyToAsync(stream);
                }
                
                // Verify file was saved
                if (!System.IO.File.Exists(path))
                {
                    TempData["ProfileMsg"] = "Không thể lưu file. Vui lòng thử lại.";
                    return RedirectToAction(nameof(Index));
                }
                
                var url = $"/uploads/avatars/{fileName}";
                
                // Update claim
                await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("avatar", url));
                
                // Refresh authentication cookie để cập nhật claims mới vào session
                await _signInManager.RefreshSignInAsync(user);
                
                // Lưu URL vào ViewBag để view có thể hiển thị ngay
                ViewBag.AvatarUrl = url;
                
                TempData["ProfileMsg"] = "Cập nhật ảnh đại diện thành công.";
                TempData["ProfileSuccess"] = "true";
                TempData["AvatarUpdated"] = "true"; // Flag để biết vừa mới update
            }
            catch (Exception ex)
            {
                TempData["ProfileMsg"] = $"Lỗi khi cập nhật avatar: {ex.Message}";
            }
            
            return RedirectToAction(nameof(Index));
        }
    }
}


