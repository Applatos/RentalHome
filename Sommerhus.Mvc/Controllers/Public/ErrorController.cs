using Microsoft.AspNetCore.Mvc;

namespace Sommerhus.Mvc.Controllers.Public;

public sealed class ErrorController : Controller
{
    [HttpGet("/error")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Index()
    {
        var statusCode = HttpContext.Response.StatusCode;
        ViewData["StatusCode"] = statusCode;
        return View();
    }

    [HttpGet("/error/{code:int}")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult StatusCodePage(int code)
    {
        ViewData["StatusCode"] = code;
        return View("Index");
    }
}
