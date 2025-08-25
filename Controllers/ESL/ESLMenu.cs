using Microsoft.AspNetCore.Mvc;

namespace Iot_dashboard.Controllers.ESL
{
    public class ESLMenu : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/ESL/ESLMenu.cshtml");
        }
    }
}
