using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sommerhus.Core.Identity;

namespace Sommerhus.Mvc.Controllers.Admin;

[Authorize(Roles = AppRoles.Admin)]
public abstract class AdminControllerBase : SommerhusControllerBase
{
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
