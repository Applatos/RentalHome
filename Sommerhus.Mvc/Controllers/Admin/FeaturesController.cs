using Microsoft.AspNetCore.Mvc;

namespace Sommerhus.Mvc.Controllers.Admin
{
    public class FeaturesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
