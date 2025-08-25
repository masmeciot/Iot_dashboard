using Microsoft.AspNetCore.Mvc;

namespace Iot_dashboard.Controllers.Iot
{
    public class iotmenuController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/iotmenu.cshtml");
        }
    }
}