using Microsoft.AspNetCore.Mvc;
using Sommerhus.Mvc.Services;

namespace Sommerhus.Mvc.Controllers;

public class HousesController(ISommerhusApi api) : Controller
{
    [HttpGet("/houses/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var h = await api.GetHouseAsync(id, ct);
        return h is null ? NotFound() : View(h);
    }
}
