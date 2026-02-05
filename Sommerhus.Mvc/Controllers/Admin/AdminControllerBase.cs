using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Sommerhus.Mvc.Controllers.Admin;

[Authorize]
public abstract class AdminControllerBase : Controller
{
    protected void SetSuccess(string message) => TempData["Ok"] = message;
    protected void SetError(string message) => TempData["Err"] = message;

    protected void SetAdminTab(string tab) => ViewData["AdminTab"] = tab;

    protected IActionResult RedirectToIndex()
        => RedirectToAction("Index");

    protected IActionResult RedirectToDetails(Guid id, string? tab = null)
        => tab is null
            ? RedirectToAction("Details", new { id })
            : RedirectToAction("Details", new { id, tab });

    protected IActionResult RedirectToEdit(Guid id)
        => RedirectToAction("Edit", new { id });
}
