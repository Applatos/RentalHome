//// Sommerhus.Mvc/Controllers/AdminCitiesController.cs
//using Microsoft.AspNetCore.Mvc;
//using Sommerhus.Mvc.Services;

//namespace Sommerhus.Mvc.Controllers;

//public class AdminCitiesController(AdminApiClient adminApi) : Controller
//{
//    [HttpGet("/admin/cities")]
//    public IActionResult Index(string? q = null, int page = 1)
//    {
//        ViewBag.Query = q;
//        ViewBag.Page = page;
//        return View("~/Views/Admin/Cities/Index.cshtml");
//    }

//    // Liste (partial)
//    [HttpGet("/admin/cities/list")]
//    public async Task<IActionResult> List(string? q, int page = 1, int pageSize = 20, CancellationToken ct = default)
//    {
//        var data = await adminApi.ListCitiesAsync(q, page, pageSize, ct) ?? new AdminApiClient.CityPage { Query = q, Page = page, PageSize = pageSize, Total = 0 };
//        return PartialView("~/Views/Admin/Cities/_List.cshtml", data);
//    }

//    // Rediger formular (partial)
//    [HttpGet("/admin/cities/{id:guid}/edit")]
//    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
//    {
//        var c = await adminApi.GetCityAsync(id, ct);
//        if (c is null) return NotFound();
//        return PartialView("~/Views/Admin/Cities/_Form.cshtml", c);
//    }

//    // Gem tekst + stamdata
//    [ValidateAntiForgeryToken]
//    [HttpPost("/admin/cities/{id:guid}")]
//    public async Task<IActionResult> Update(Guid id, string name, string zip, string? slug, string? text, string? q, int page = 1, CancellationToken ct = default)
//    {
//        await adminApi.UpdateCityAsync(id, name, zip, slug, text, ct);
//        return await List(q, page, 20, ct);
//    }

//    // Upload billede
//    [ValidateAntiForgeryToken]
//    [HttpPost("/admin/cities/{id:guid}/images")]
//    public async Task<IActionResult> Upload(Guid id, IFormFile file, CancellationToken ct)
//    {
//        if (file is { Length: > 0 })
//        {
//            using var s = file.OpenReadStream();
//            await adminApi.UploadCityImageAsync(id, s, file.FileName, ct);
//        }
//        return await Edit(id, ct);
//    }

//    // Slet billede
//    [ValidateAntiForgeryToken]
//    [HttpPost("/admin/cities/{id:guid}/images/{imageId:guid}/delete")]
//    public async Task<IActionResult> DeleteImage(Guid id, Guid imageId, CancellationToken ct)
//    {
//        await adminApi.DeleteCityImageAsync(id, imageId, ct);
//        return await Edit(id, ct);
//    }

//    // Slet city
//    [ValidateAntiForgeryToken]
//    [HttpPost("/admin/cities/{id:guid}/delete")]
//    public async Task<IActionResult> Delete(Guid id, string? q, int page = 1, CancellationToken ct = default)
//    {
//        await adminApi.DeleteCityAsync(id, ct);
//        return await List(q, page, 20, ct);
//    }
//}
