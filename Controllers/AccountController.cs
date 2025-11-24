using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webxemphim.Data;
using webxemphim.Models;
using webxemphim.Services;
using System.Security.Claims;

namespace webxemphim.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly webxemphim.Services.AuditLogService _auditLog;
        private readonly CoinService _coinService;
        private readonly AchievementService _achievementService;
        private readonly ApplicationDbContext _db;

        public AccountController(
            SignInManager<ApplicationUser> signInManager, 
            UserManager<ApplicationUser> userManager,
            webxemphim.Services.AuditLogService auditLog,
            CoinService coinService,
            AchievementService achievementService,
            ApplicationDbContext db)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _auditLog = auditLog;
            _coinService = coinService;
            _achievementService = achievementService;
            _db = db;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(string.Empty, "Thông tin không hợp lệ.");
                return View();
            }
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Tài khoản không tồn tại.");
                return View();
            }
            
            // Kiểm tra nếu tài khoản bị lockout
            if (await _userManager.IsLockedOutAsync(user))
            {
                ModelState.AddModelError(string.Empty, "Tài khoản của bạn đã bị dừng hoạt động. Vui lòng liên hệ quản trị viên.");
                return View();
            }
            
            var result = await _signInManager.PasswordSignInAsync(user, password, isPersistent: true, lockoutOnFailure: false);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            
            if (result.Succeeded)
            {
                await _auditLog.LogLoginAsync(user.Id, true, ipAddress);
                
                // Xử lý đăng nhập hàng ngày và login streak
                await ProcessDailyLoginRewardAsync(user);
                
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
                return RedirectToAction("Index", "Home");
            }
            if (result.RequiresTwoFactor)
            {
                return RedirectToAction(nameof(Login2FA), new { returnUrl, rememberMe = true });
            }
            if (result.IsLockedOut)
            {
                await _auditLog.LogLoginAsync(user.Id, false, ipAddress, "Account locked out");
                ModelState.AddModelError(string.Empty, "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.");
                return View();
            }
            await _auditLog.LogLoginAsync(user.Id, false, ipAddress, "Invalid password");
            ModelState.AddModelError(string.Empty, "Đăng nhập thất bại. Email hoặc mật khẩu không đúng.");
            return View();
        }

        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string email, string displayName, string password, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            
            // Validation
            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(string.Empty, "Email là bắt buộc.");
                return View();
            }
            
            if (string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(string.Empty, "Mật khẩu là bắt buộc.");
                return View();
            }
            
            if (password.Length < 6)
            {
                ModelState.AddModelError(string.Empty, "Mật khẩu phải có ít nhất 6 ký tự.");
                return View();
            }
            
            // Check if email already exists
            var exists = await _userManager.FindByEmailAsync(email);
            if (exists != null)
            {
                ModelState.AddModelError(string.Empty, "Email này đã được sử dụng. Vui lòng chọn email khác.");
                return View();
            }
            
            // Create user
            var user = new ApplicationUser 
            { 
                UserName = email, 
                Email = email,
                EmailConfirmed = true // Tự động xác nhận email để có thể đăng nhập ngay
            }; 
            
            var create = await _userManager.CreateAsync(user, password);
            if (create.Succeeded)
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                await _auditLog.LogCrudAsync("CREATE", "User", 0, user.Id, "User registered", ipAddress);
                // Sign in user
                await _signInManager.SignInAsync(user, isPersistent: true);
                await _auditLog.LogLoginAsync(user.Id, true, ipAddress);
                
                // Redirect
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) 
                    return Redirect(returnUrl);
                return RedirectToAction("Index", "Home");
            }
            
            // Handle errors
            foreach (var error in create.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _signInManager.SignOutAsync();
            if (!string.IsNullOrEmpty(userId))
            {
                await _auditLog.LogAsync("LOGOUT", userId, "User", null, "User logged out", ipAddress);
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
        {
            returnUrl ??= Url.Content("~/");
            if (!string.IsNullOrEmpty(remoteError))
            {
                TempData["Error"] = $"Lỗi đăng nhập: {remoteError}";
                return RedirectToAction(nameof(Login), new { returnUrl });
            }
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToAction(nameof(Login), new { returnUrl });
            }
            var signInResult = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: true);
            if (signInResult.Succeeded)
            {
                // Xử lý đăng nhập hàng ngày cho external login
                var email = info.Principal.FindFirstValue(System.Security.Claims.ClaimTypes.Email);
                if (!string.IsNullOrEmpty(email))
                {
                    var user = await _userManager.FindByEmailAsync(email);
                    if (user != null)
                    {
                        await ProcessDailyLoginRewardAsync(user);
                    }
                }
                return LocalRedirect(returnUrl);
            }

            // Create user if not exists
            var email2 = info.Principal.FindFirstValue(System.Security.Claims.ClaimTypes.Email);
            if (email2 == null)
            {
                TempData["Error"] = "Không lấy được email từ nhà cung cấp.";
                return RedirectToAction(nameof(Login), new { returnUrl });
            }
            var user2 = await _userManager.FindByEmailAsync(email2);
            if (user2 == null)
            {
                user2 = new ApplicationUser { UserName = email2, Email = email2 };
                var createResult = await _userManager.CreateAsync(user2);
                if (!createResult.Succeeded)
                {
                    TempData["Error"] = string.Join("; ", createResult.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(Login), new { returnUrl });
                }
            }
            await _userManager.AddLoginAsync(user2, info);
            await _signInManager.SignInAsync(user2, isPersistent: true);
            
            // Xử lý đăng nhập hàng ngày
            await ProcessDailyLoginRewardAsync(user2);
            
            return LocalRedirect(returnUrl);
        }
        
        private async Task ProcessDailyLoginRewardAsync(ApplicationUser user)
        {
            var today = DateTime.UtcNow.Date;
            var lastLoginDate = user.LastLoginDate?.Date;
            
            // Cập nhật login streak
            if (lastLoginDate == null || lastLoginDate < today.AddDays(-1))
            {
                user.LoginStreak = 1;
            }
            else if (lastLoginDate == today.AddDays(-1))
            {
                user.LoginStreak++;
            }
            
            // Cập nhật last login
            user.LastLoginAt = DateTime.UtcNow;
            user.LastLoginDate = today;
            await _userManager.UpdateAsync(user);
            
            // Thưởng coin đăng nhập hàng ngày (chỉ 1 lần mỗi ngày)
            if (lastLoginDate != today)
            {
                await _coinService.RewardDailyLoginAsync(user.Id);
                await _achievementService.CheckAndUpdateAchievementsAsync(user.Id, AchievementType.DaysActive);
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Enable2FA()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");
            var key = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(key))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                key = await _userManager.GetAuthenticatorKeyAsync(user);
            }
            var email = await _userManager.GetEmailAsync(user) ?? user.UserName ?? "user";
            var issuer = "bmovie";
            var otpauth = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}?secret={key}&issuer={Uri.EscapeDataString(issuer)}&digits=6";
            ViewBag.Key = key;
            ViewBag.Qr = $"https://chart.googleapis.com/chart?cht=qr&chs=200x200&chl={Uri.EscapeDataString(otpauth)}";
            ViewBag.IsEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enable2FA(string code)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");
            code = (code ?? string.Empty).Replace(" ", string.Empty).Replace("-", string.Empty);
            var isValid = await _userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, code);
            if (!isValid)
            {
                TempData["Error"] = "Mã xác thực không đúng.";
                return RedirectToAction(nameof(Enable2FA));
            }
            await _userManager.SetTwoFactorEnabledAsync(user, true);
            TempData["Msg"] = "Đã bật xác thực 2 lớp.";
            return RedirectToAction(nameof(Enable2FA));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Disable2FA()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");
            await _userManager.SetTwoFactorEnabledAsync(user, false);
            await _userManager.ResetAuthenticatorKeyAsync(user);
            TempData["Msg"] = "Đã tắt 2FA cho tài khoản.";
            return RedirectToAction(nameof(Enable2FA));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateRecoveryCodes()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");
            if (!await _userManager.GetTwoFactorEnabledAsync(user))
            {
                TempData["Error"] = "Bạn cần bật 2FA trước khi tạo mã khôi phục.";
                return RedirectToAction(nameof(Enable2FA));
            }
            var codes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            ViewBag.Codes = codes.ToArray();
            return View("RecoveryCodes");
        }

        [HttpGet]
        public IActionResult Login2FA(string? returnUrl = null, bool rememberMe = true)
        {
            ViewData["ReturnUrl"] = returnUrl;
            ViewData["RememberMe"] = rememberMe;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login2FA(string code, bool rememberMe = true, string? returnUrl = null)
        {
            var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(code.Replace(" ", string.Empty).Replace("-", string.Empty), rememberMe, rememberClient: true);
            if (result.Succeeded)
            {
                // Xử lý đăng nhập hàng ngày cho 2FA login
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    await ProcessDailyLoginRewardAsync(user);
                }
                
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
                return RedirectToAction("Index", "Home");
            }
            ModelState.AddModelError(string.Empty, "Mã 2FA không đúng.");
            return View();
        }
    }
}


