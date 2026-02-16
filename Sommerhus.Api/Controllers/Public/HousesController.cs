using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Sommerhus.Api.Infrastructure;
using Sommerhus.Core.Services.Public.Houses;
using Sommerhus.Core.Services.Public.Favorites;
using Sommerhus.Core.Services.Public.Pricing;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Api.Controllers.Public;

[ApiController]
[Route("api/[controller]")]
public sealed class HousesController(IHouseQueryService houses, IFavoriteService favoriteService, IPricingQuoteService pricingQuoteService) : ControllerBase
{
    [HttpGet]
    public Task<PageResult<PublicHouseListItemDto>> Search(
        [FromQuery(Name = "q")] string? query,
        [FromQuery] string? city,
        [FromQuery] Guid? area,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] int? minBedrooms,
        [FromQuery] int? minGuests,
        [FromQuery] bool? hasPool,
        [FromQuery] bool? petFriendly,
        [FromQuery] HouseSearchSort sort = HouseSearchSort.Relevance,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var featureFilters = Request.Query
            .Where(kvp => kvp.Key.StartsWith("f_", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(
                kvp => kvp.Key[2..],
                kvp => kvp.Value.ToString(),
                StringComparer.OrdinalIgnoreCase);

        var filter = new HouseSearchFilter
        {
            Query = query,
            City = city,
            AreaId = area,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            MinBedrooms = minBedrooms,
            MinGuests = minGuests,
            HasPool = hasPool,
            PetFriendly = petFriendly,
            FeatureFilters = featureFilters.Count == 0 ? null : featureFilters,
            Sort = sort,
            Page = page,
            PageSize = pageSize
        };

        return houses.SearchAsync(filter, Request.BaseUrl(), ct);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PublicHouseDetailsDto>> Get(Guid id, CancellationToken ct)
    {
        var house = await houses.GetAsync(id, Request.BaseUrl(), ct);
        if (house is null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is not null)
        {
            var isFav = await favoriteService.IsFavoritedAsync(userId, id, ct);
            house = house with { IsFavorite = isFav };
        }

        return house;
    }

    [HttpGet("{id:guid}/quote")]
    public async Task<ActionResult<PriceQuoteResponseDto>> Quote(
        Guid id,
        [FromQuery] DateOnly checkIn,
        [FromQuery] DateOnly checkOut,
        [FromQuery] int guests = 2,
        CancellationToken ct = default)
    {
        var request = new PriceQuoteRequestDto(id, checkIn, checkOut, guests, null);
        var result = await pricingQuoteService.QuoteAsync(request, ct);
        return this.FromResult(result);
    }
}
