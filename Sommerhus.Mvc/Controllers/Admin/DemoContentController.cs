using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Sommerhus.Mvc.Infrastructure;
using Sommerhus.Mvc.Services;

namespace Sommerhus.Mvc.Controllers.Admin;

public sealed class DemoContentController(AdminApiClient api, IStringLocalizer<SharedResource> localizer) : AdminControllerBase
{
    [HttpPost("/admin/demo-content/import")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(CancellationToken ct = default)
    {
        var res = await api.ImportDemoContentAsync(ct);
        if (!res.Ok || res.Data is null)
        {
            SetError($"{localizer["Admin.DemoContent.Failed"]} {ApiErrorText.Describe(res, string.Empty)}".TrimEnd());
            return RedirectToIndex();
        }

        var result = res.Data;
        var details = string.Join("; ", result.Changes.Concat(result.Notes));
        if (!result.Succeeded)
        {
            SetError($"{localizer["Admin.DemoContent.Failed"]} {result.Error} {details}".TrimEnd());
        }
        else if (result.Changes.Count == 0)
        {
            SetSuccess($"{localizer["Admin.DemoContent.NothingToDo"]} {string.Join("; ", result.Notes)}".TrimEnd());
        }
        else
        {
            SetSuccess($"{localizer["Admin.DemoContent.Imported"]} {details}");
        }

        return RedirectToIndex();
    }
}
