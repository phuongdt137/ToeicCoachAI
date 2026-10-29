using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using ToeicCoachAI.MVC.Models;

namespace ToeicCoachAI.MVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index(bool landing = false)
        {
            if (User.Identity?.IsAuthenticated == true && !landing)
            {
                if (User.IsInRole("SYSTEM_ADMIN"))
                {
                    return RedirectToAction("Index", "Admin");
                }

                return RedirectToAction("Index", "Dashboard");
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
