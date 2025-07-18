using Microsoft.AspNetCore.Mvc;

namespace Iot_dashboard.Controllers.Iot
{
    public class iotmainmenu : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/iotmainmenu.cshtml");
        }
    }
}
