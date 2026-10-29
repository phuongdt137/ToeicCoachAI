using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using ToeicCoachAI.Model.DTOs.Auth;
using ToeicCoachAI.Service.Interfaces;

namespace ToeicCoachAI.MVC.Controllers
{
    public class AuthController : Controller
    {
        public const string GuestTokenCookieName = "tc_guest_token";
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;

        public AuthController(IAuthService authService, IConfiguration configuration)
        {
            _authService = authService;
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToLocal(returnUrl);
            }

            return View(new LoginRequestDto { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginRequestDto model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.AnonymousGuestToken = Request.Cookies[GuestTokenCookieName];

            var result = await _authService.LoginAsync(model, cancellationToken);
            if (!result.IsSuccess || result.Data == null)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            await SignInUserCookieAsync(result.Data, model.RememberMe);
            ClearGuestTokenCookieIfPresent();

            return RedirectToLocal(model.ReturnUrl, result.Data.Role == ToeicCoachAI.Model.Enums.UserRole.SYSTEM_ADMIN);
        }

        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToLocal(returnUrl, User.IsInRole("SYSTEM_ADMIN"));
            }

            return View(new RegisterRequestDto { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterRequestDto model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.AnonymousGuestToken = Request.Cookies[GuestTokenCookieName];

            var result = await _authService.RegisterAsync(model, cancellationToken);
            if (!result.IsSuccess || result.Data == null)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            await SignInUserCookieAsync(result.Data, isPersistent: true);
            ClearGuestTokenCookieIfPresent();

            return RedirectToLocal(model.ReturnUrl, result.Data.Role == ToeicCoachAI.Model.Enums.UserRole.SYSTEM_ADMIN);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GoogleLogin(string? returnUrl = null)
        {
            var clientId = _configuration["Authentication:Google:ClientId"];
            if (string.IsNullOrWhiteSpace(clientId) || clientId.StartsWith("YOUR_GOOGLE_CLIENT_ID"))
            {
                TempData["AuthError"] = "Chưa cấu hình Google ClientId & ClientSecret trong appsettings.json.";
                return RedirectToAction(nameof(Login), new { returnUrl });
            }

            var redirectUrl = Url.Action(nameof(GoogleCallback), "Auth", new { returnUrl });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public async Task<IActionResult> GoogleCallback(string? returnUrl = null, string? remoteError = null, CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrWhiteSpace(remoteError))
            {
                TempData["AuthError"] = $"Lỗi xác thực từ Google: {remoteError}";
                return RedirectToAction(nameof(Login), new { returnUrl });
            }

            var authResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!authResult.Succeeded || authResult.Principal == null)
            {
                TempData["AuthError"] = "Không thể xác thực thông tin từ tài khoản Google.";
                return RedirectToAction(nameof(Login), new { returnUrl });
            }

            var email = authResult.Principal.FindFirstValue(ClaimTypes.Email);
            var fullName = authResult.Principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["AuthError"] = "Tài khoản Google không cung cấp địa chỉ Email.";
                return RedirectToAction(nameof(Login), new { returnUrl });
            }

            var googleRequest = new GoogleAuthRequestDto
            {
                Email = email,
                FullName = fullName,
                AnonymousGuestToken = Request.Cookies[GuestTokenCookieName]
            };

            var serviceResult = await _authService.AuthenticateGoogleAsync(googleRequest, cancellationToken);
            if (!serviceResult.IsSuccess || serviceResult.Data == null)
            {
                TempData["AuthError"] = serviceResult.Message;
                return RedirectToAction(nameof(Login), new { returnUrl });
            }

            await SignInUserCookieAsync(serviceResult.Data, isPersistent: true);
            ClearGuestTokenCookieIfPresent();

            return RedirectToLocal(returnUrl, serviceResult.Data.Role == ToeicCoachAI.Model.Enums.UserRole.SYSTEM_ADMIN);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        private async Task SignInUserCookieAsync(AuthUserResponseDto user, bool isPersistent)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role.ToString()),
                new("Tier", user.Tier.ToString()),
                new("CurrentTier", user.Tier.ToString()),
                new("CreditBalance", user.CreditBalance.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }

        private void ClearGuestTokenCookieIfPresent()
        {
            if (Request.Cookies.ContainsKey(GuestTokenCookieName))
            {
                Response.Cookies.Delete(GuestTokenCookieName);
            }
        }

        private IActionResult RedirectToLocal(string? returnUrl, bool isAdmin = false)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            if (isAdmin || User.IsInRole("SYSTEM_ADMIN"))
            {
                return RedirectToAction("Index", "Admin");
            }

            return RedirectToAction("Index", "Dashboard");
        }
    }
}
