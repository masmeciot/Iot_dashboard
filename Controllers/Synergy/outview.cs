using Microsoft.AspNetCore.Mvc;

namespace Iot_dashboard.Controllers.Synergy
{
    public class outview : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/synergy/outview.cshtml");
        }
    }
}
