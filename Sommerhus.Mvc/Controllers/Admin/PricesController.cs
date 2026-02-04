using Microsoft.AspNetCore.Mvc;

namespace Sommerhus.Mvc.Controllers.Admin
{
    public class PricesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
