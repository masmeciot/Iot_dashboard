using Microsoft.AspNetCore.Mvc;

namespace Iot_dashboard.Controllers.Iot.LOGIN
{
    public class login : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/LOGIN/login.cshtml");
        }
    }
}
