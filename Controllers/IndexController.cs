using Microsoft.AspNetCore.Mvc;

namespace Iot_dashboard.Controllers.IndexManagers
{
    public class IndexM : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Index.cshtml");
        }
    }
}
