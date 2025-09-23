using Microsoft.AspNetCore.Mvc;
using Sommerhus.Mvc.Services;
using System.Collections.Generic;
using System.Linq;

namespace Sommerhus.Mvc.Controllers;

public class HomeController(ISommerhusApi api) : Controller
{
    public record HomeIndexVM(
        string? SelectedCitySlug,
        string? Q,
        IReadOnlyList<CityListItem> Cities,
        IReadOnlyList<CardItem> Houses);

    public record CardItem(
        Guid Id, string Title, string? Subtitle, string? City, string? Zip,
        string? Cover, List<string> Thumbs);

    [HttpGet("/")]
    public async Task<IActionResult> Index([FromQuery] string? city, [FromQuery] string? q, CancellationToken ct)
    {
        var cities = await api.GetCitiesAsync(ct);

        // hent hus-liste (nyeste først) og suppler med thumbs via detaljer (N+1 – fint for forside)
        var list = await api.GetHousesAsync(city, q, ct);

        // hent detaljer parallelt for at få galleri-thumbs
        var detailsTasks = list.Select(h => api.GetHouseAsync(h.Id, ct)).ToList();
        var details = await Task.WhenAll(detailsTasks);

        var items = new List<CardItem>();
        for (int i = 0; i < list.Count; i++)
        {
            var h = list[i];
            var d = details[i];
            var thumbs = new List<string>();
            if (d?.Gallery is { Length: > 0 })
            {
                thumbs.AddRange(d.Gallery.Take(5).Select(g => g.Url));
            }

            var cover = string.IsNullOrWhiteSpace(h.Cover)
                ? (d?.CoverUrl ?? thumbs.FirstOrDefault())
                : h.Cover;

            items.Add(new CardItem(h.Id, h.Title, h.Subtitle, h.City, h.Zip, cover, thumbs));
        }

        return View(new HomeIndexVM(city, q, cities, items));
    }
}
