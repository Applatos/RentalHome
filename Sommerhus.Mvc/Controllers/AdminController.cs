using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Sommerhus.Mvc.Models.Admin;
using Sommerhus.Mvc.Services;
using AdmAreas = Sommerhus.Contracts.Dtos.Admin.Areas;
using AdmHouses = Sommerhus.Contracts.Dtos.Admin.Houses;

namespace Sommerhus.Mvc.Controllers;

public sealed class AdminController(AdminApiClient admin) : Controller
{
    private const string AreaSuccessKey = "AdminAreaSuccess";
    private const string AreaErrorKey = "AdminAreaError";

    [HttpGet("/admin")]
    public IActionResult Index() => RedirectToAction(nameof(Houses));

    [HttpGet("/admin/houses")]
    public async Task<IActionResult> Houses([FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 50);
        AdmHouses.HousesPageDto res = await admin.SearchHousesAsync(q, page, pageSize, ct);
        return View(res);
    }

    [HttpGet("/admin/houses/{id:guid}")]
    public async Task<IActionResult> House(Guid id, CancellationToken ct)
    {
        var dto = await admin.GetHouseAsync(id, ct);
        if (dto is null) return NotFound();
        return View(dto);
    }

    [HttpGet("/admin/areas")]
    public async Task<IActionResult> Areas(CancellationToken ct)
    {
        ViewBag.AreaSuccess = TempData[AreaSuccessKey] as string;
        ViewBag.AreaError = TempData[AreaErrorKey] as string;
        var areas = await admin.GetAreasAsync(ct);
        return View(areas);
    }

    [HttpGet("/admin/areas/create")]
    public async Task<IActionResult> CreateArea(CancellationToken ct)
    {
        var vm = await BuildAreaFormAsync(new AreaFormInput(), "Opret område", "Opret", null, null, ct);
        return View("~/Views/Admin/Areas/Create.cshtml", vm);
    }

    [HttpPost("/admin/areas/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateArea(AreaFormInput input, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            var vm = await BuildAreaFormAsync(input, "Opret område", "Opret", null, null, ct);
            return View("~/Views/Admin/Areas/Create.cshtml", vm);
        }

        var dto = new AdmAreas.CreateAreaDto(input.Name!, input.CityId, input.Description);
        var result = await admin.CreateAreaAsync(dto, ct);

        if (!result.Ok)
        {
            if (result.HasValidationErrors && result.Errors is not null)
            {
                ApplyErrors(result.Errors);
            }
            else
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Kunne ikke oprette området.");
            }

            var vm = await BuildAreaFormAsync(input, "Opret område", "Opret", null, null, ct);
            return View("~/Views/Admin/Areas/Create.cshtml", vm);
        }

        var created = result.Payload;
        if (created is null)
        {
            ModelState.AddModelError(string.Empty, "API returnerede ingen data.");
            var vmMissing = await BuildAreaFormAsync(input, "Opret område", "Opret", null, null, ct);
            return View("~/Views/Admin/Areas/Create.cshtml", vmMissing);
        }

        TempData[AreaSuccessKey] = "Området er oprettet.";
        return RedirectToAction(nameof(Area), new { id = created.Id });
    }

    [HttpGet("/admin/areas/{id:guid}")]
    public async Task<IActionResult> Area(Guid id, CancellationToken ct)
    {
        var dto = await admin.GetAreaAsync(id, ct);
        if (dto is null) return NotFound();
        ViewBag.AreaSuccess = TempData[AreaSuccessKey] as string;
        ViewBag.AreaError = TempData[AreaErrorKey] as string;
        return View(dto);
    }

    [HttpGet("/admin/areas/{id:guid}/edit")]
    public async Task<IActionResult> EditArea(Guid id, CancellationToken ct)
    {
        var dto = await admin.GetAreaAsync(id, ct);
        if (dto is null) return NotFound();

        var input = new AreaFormInput
        {
            Name = dto.Name,
            CityId = dto.CityId,
            Description = dto.Description
        };

        var vm = await BuildAreaFormAsync(input, "Redigér område", "Gem", dto.Id, dto.Slug, ct);
        return View("~/Views/Admin/Areas/Edit.cshtml", vm);
    }

    [HttpPost("/admin/areas/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditArea(Guid id, AreaFormInput input, CancellationToken ct)
    {
        var dto = await admin.GetAreaAsync(id, ct);
        if (dto is null) return NotFound();

        if (!ModelState.IsValid)
        {
            var vmInvalid = await BuildAreaFormAsync(input, "Redigér område", "Gem", dto.Id, dto.Slug, ct);
            return View("~/Views/Admin/Areas/Edit.cshtml", vmInvalid);
        }

        var updateDto = new AdmAreas.UpdateAreaDto(input.Name!, input.CityId, input.Description);
        var result = await admin.UpdateAreaAsync(id, updateDto, ct);

        if (!result.Ok)
        {
            if (result.HasValidationErrors && result.Errors is not null)
            {
                ApplyErrors(result.Errors);
            }
            else
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Kunne ikke opdatere området.");
            }

            var vmError = await BuildAreaFormAsync(input, "Redigér område", "Gem", dto.Id, dto.Slug, ct);
            return View("~/Views/Admin/Areas/Edit.cshtml", vmError);
        }

        TempData[AreaSuccessKey] = "Området er opdateret.";
        return RedirectToAction(nameof(Area), new { id });
    }

    [HttpPost("/admin/areas/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteArea(Guid id, CancellationToken ct)
    {
        var result = await admin.DeleteAreaAsync(id, ct);
        if (!result.Ok && result.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            TempData[AreaErrorKey] = result.ErrorMessage ?? "Kunne ikke slette området.";
            return RedirectToAction(nameof(Area), new { id });
        }

        TempData[AreaSuccessKey] = "Området er slettet.";
        return RedirectToAction(nameof(Areas));
    }

    [HttpGet("/admin/features")]
    public async Task<IActionResult> Features(CancellationToken ct)
        => View(await admin.GetFeaturesAsync(ct));

    private async Task<AreaFormViewModel> BuildAreaFormAsync(AreaFormInput input, string title, string submitText, Guid? id, string? slug, CancellationToken ct)
    {
        var cities = await admin.GetCitiesAsync(ct);
        var options = new List<SelectListItem>();
        foreach (var city in cities.OrderBy(c => c.Name))
        {
            var label = string.IsNullOrWhiteSpace(city.Zip)
                ? city.Name
                : $"{city.Name} ({city.Zip})";
            options.Add(new SelectListItem(label, city.Id.ToString(), input.CityId.HasValue && input.CityId.Value == city.Id));
        }

        return new AreaFormViewModel
        {
            Id = id,
            Slug = slug,
            Title = title,
            SubmitText = submitText,
            Input = input,
            Cities = options
        };
    }

    private void ApplyErrors(IReadOnlyDictionary<string, string[]> errors)
    {
        foreach (var kvp in errors)
        {
            var targetKey = string.IsNullOrEmpty(kvp.Key)
                ? string.Empty
                : (kvp.Key.StartsWith("Input.", StringComparison.OrdinalIgnoreCase) ? kvp.Key : $"Input.{kvp.Key}");

            foreach (var error in kvp.Value)
            {
                ModelState.AddModelError(targetKey, error);
            }
        }
    }
}
