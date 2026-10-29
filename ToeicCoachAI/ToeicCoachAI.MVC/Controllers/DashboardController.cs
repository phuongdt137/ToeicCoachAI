using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToeicCoachAI.Service.Interfaces;

namespace ToeicCoachAI.MVC.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var result = await _dashboardService.GetStudentDashboardAsync(userId, cancellationToken);
            if (!result.IsSuccess || result.Data == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            return View(result.Data);
        }

        [HttpGet]
        public async Task<IActionResult> Practice(CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var result = await _dashboardService.GetStudentDashboardAsync(userId, cancellationToken);
            if (!result.IsSuccess || result.Data == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            return View(result.Data);
        }

        [HttpGet]
        public async Task<IActionResult> Tests(CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var result = await _dashboardService.GetTestHubPageAsync(userId, cancellationToken);
            if (!result.IsSuccess || result.Data == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            return View(result.Data);
        }

        [HttpGet]
        public async Task<IActionResult> Vocabulary(CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var result = await _dashboardService.GetVocabHubPageAsync(userId, cancellationToken);
            if (!result.IsSuccess || result.Data == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            return View(result.Data);
        }

        [HttpGet]
        public async Task<IActionResult> Progress(CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var result = await _dashboardService.GetStudentDashboardAsync(userId, cancellationToken);
            if (!result.IsSuccess || result.Data == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            return View(result.Data);
        }

        [HttpGet]
        public async Task<IActionResult> Subscription(CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var result = await _dashboardService.GetSubscriptionAndWalletPageAsync(userId, cancellationToken);
            if (!result.IsSuccess || result.Data == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            return View(result.Data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PurchasePlan(Guid planId, CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var result = await _dashboardService.PurchaseSubscriptionPlanAsync(userId, planId, cancellationToken);
            if (result.IsSuccess && result.Data != null)
            {
                var user = result.Data;
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
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14)
                    });

                TempData["SyncedGuestMessage"] = result.Message;
            }
            else
            {
                TempData["SyncedGuestMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Subscription));
        }

        private bool TryGetCurrentUserId(out Guid userId)
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(idClaim, out userId);
        }
    }
}
