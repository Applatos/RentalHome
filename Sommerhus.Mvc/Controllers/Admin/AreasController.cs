using Microsoft.AspNetCore.Mvc;

namespace Sommerhus.Mvc.Controllers.Admin
{
    public class AreasController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
