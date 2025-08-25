using Microsoft.AspNetCore.Mvc;

namespace Iot_dashboard.Controllers.AMR
{
    public class AMRMenuController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/AMR/AMRMenu.cshtml");
        }
    }
}
