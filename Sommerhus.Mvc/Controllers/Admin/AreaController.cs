using Microsoft.AspNetCore.Mvc;

namespace Sommerhus.Mvc.Controllers.Admin
{
    public class AreaController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
