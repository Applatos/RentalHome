using Microsoft.AspNetCore.Mvc;

namespace Sommerhus.Mvc.Controllers;

public abstract class SommerhusControllerBase : Controller
{
    protected void SetSuccess(string message) => TempData["Ok"] = message;
    protected void SetError(string message) => TempData["Err"] = message;
}
