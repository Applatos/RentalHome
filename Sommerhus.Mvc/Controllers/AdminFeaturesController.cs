//using Microsoft.AspNetCore.Mvc;
//using Sommerhus.Mvc.Services;

//namespace Sommerhus.Mvc.Controllers;

//public class AdminFeaturesController(SommerhusApi api) : Controller
//{
//    [HttpGet("/admin/{houseId:guid}/features")]
//    public async Task<IActionResult> Edit(Guid houseId, CancellationToken ct)
//    {
//        var house = await api.GetHouseAsync(houseId, ct);
//        if (house is null) return NotFound();

//        var master = await api.GetFeaturesAsync(ct);
//        var current = await api.GetHouseFeaturesAsync(houseId, ct);

//        var vm = new EditFeaturesVm(houseId, house.Title, master, current);
//        return View(vm);
//    }

//    public record EditFeaturesVm(Guid HouseId, string HouseTitle,
//        IReadOnlyList<FeatureDto> Master, IReadOnlyList<FeatureValueDto> Current);

//    public class FeaturePostModel { public List<FeaturePostItem> Items { get; set; } = new(); }
//    public class FeaturePostItem
//    {
//        public Guid FeatureId { get; set; }
//        public bool? ValueBool { get; set; }
//        public int? ValueInt { get; set; }
//        public decimal? ValueDecimal { get; set; }
//        public string? ValueText { get; set; }
//    }

//    [HttpPost("/admin/{houseId:guid}/features")]
//    public async Task<IActionResult> Save(Guid houseId, FeaturePostModel model, CancellationToken ct)
//    {
//        // Behandl "unchecked" som false
//        var values = model.Items.Select(i =>
//            new CreateHouseFeatureValueDto(
//                i.FeatureId,
//                i.ValueBool ?? false,
//                i.ValueInt,
//                i.ValueDecimal,
//                i.ValueText));

//        await api.UpsertHouseFeaturesAsync(houseId, values, ct);
//        TempData["ok"] = "Features opdateret";
//        return Redirect($"/admin/{houseId}/features");
//    }
//}
