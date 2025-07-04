using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace Iot_dashboard.Controllers.GM
{
    public class GMController : Controller
    {
        // This action will handle /GM
        public IActionResult Index()
        {
            // Check for session variable set on successful login
            var token = HttpContext.Session.GetString("accessToken");
            if (string.IsNullOrEmpty(token))
            {
                // Not logged in, show login page
                return View("GMLogin");
            }

            // Logged in, show the menu
            return View("GMIndex");
        }
    }
}