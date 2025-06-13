using Microsoft.AspNetCore.Mvc;

namespace Iot_dashboard.Controllers.Synergy
{
    public class synmenu : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/synergy/synmenu.cshtml");
        }

    }
}


