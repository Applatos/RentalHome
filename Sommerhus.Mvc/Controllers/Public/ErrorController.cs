using Microsoft.AspNetCore.Mvc;

namespace Sommerhus.Mvc.Controllers.Public;

public sealed class ErrorController : Controller
{
    [Route("/error")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Index()
    {
        var statusCode = HttpContext.Response.StatusCode;
        ViewData["StatusCode"] = statusCode;
        return View();
    }

    [Route("/error/{code:int}")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult StatusCodePage(int code)
    {
        ViewData["StatusCode"] = code;
        return View("Index");
    }
}
