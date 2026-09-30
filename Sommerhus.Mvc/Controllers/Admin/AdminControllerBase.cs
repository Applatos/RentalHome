using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sommerhus.Core.Identity;

namespace Sommerhus.Mvc.Controllers.Admin;

/// <remarks>
/// Houses and Areas exist both as public and as admin controllers. The "Admin" area tells them
/// apart when links are generated: admin pages keep the area as an ambient value, so a plain
/// <c>asp-controller="Houses"</c> stays in admin there and reaches the public page everywhere
/// else. Views are still addressed by full path, and URLs come from the attribute routes, so the
/// area changes neither.
/// </remarks>
[Area(AreaName)]
[Authorize(Roles = AppRoles.Admin)]
public abstract class AdminControllerBase : SommerhusControllerBase
{
    public const string AreaName = "Admin";

    protected void SetAdminTab(string tab) => ViewData["AdminTab"] = tab;

    protected IActionResult RedirectToIndex()
        => RedirectToAction("Index", "Houses", new { area = AreaName });

    protected IActionResult RedirectToDetails(Guid id, string? tab = null)
        => tab is null
            ? RedirectToAction("Details", "Houses", new { area = AreaName, id })
            : RedirectToAction("Details", "Houses", new { area = AreaName, id, tab });

    // Houses have no separate edit page: the overview tab is the edit form.
    protected IActionResult RedirectToEdit(Guid id)
        => RedirectToDetails(id, "overview");
}
