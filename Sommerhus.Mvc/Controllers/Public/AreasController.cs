//using Microsoft.AspNetCore.Mvc;
//using Sommerhus.Mvc.Services;

//namespace Sommerhus.Mvc.Controllers.Public;

//public sealed class AreasController(SommerhusApi api) : Controller
//{
//    [HttpGet("/areas/{slug}")]
//    public async Task<IActionResult> Details(string slug, CancellationToken ct)
//    {
//        var dto = await api.GetAreaBySlugAsync(slug, ct);
//        if (dto is null) return NotFound();
//        return View(dto);
//    }
//}
