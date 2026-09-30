using System.Globalization;
using System.Net;
using FluentAssertions;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Mvc.Services;

namespace Sommerhus.Api.Tests.Mvc;

/// <summary>
/// The MVC API clients put numbers and dates in URLs invariantly, whatever the request culture:
/// the API reads "700,5" as 7005.
/// </summary>
public sealed class ApiClientUrlTests
{
    private static readonly Guid HouseId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    [Theory]
    [InlineData("da-DK")]
    [InlineData("en-GB")]
    public async Task HouseSearch_SendsPricesInvariantly(string culture)
    {
        var url = await CaptureAsync(culture, http => new SommerhusApi(http).GetHousesAsync(new HouseSearchFilter
        {
            MinPrice = 700.5m,
            MaxPrice = 1250.75m,
            MinBedrooms = 2,
            MinGuests = 4,
            Page = 2,
            PageSize = 10
        }));

        url.Should().Be("/api/houses?minPrice=700.5&maxPrice=1250.75&minBedrooms=2&minGuests=4&page=2&pageSize=10");
    }

    [Theory]
    [InlineData("da-DK")]
    [InlineData("en-GB")]
    public async Task QuoteAndAvailability_SendIsoDates(string culture)
    {
        var quote = await CaptureAsync(culture, http => new SommerhusApi(http).GetPriceQuoteAsync(
            new PriceQuoteRequestDto(HouseId, new DateOnly(2026, 7, 4), new DateOnly(2026, 7, 11), 4, null)));
        var availability = await CaptureAsync(culture, http => new SommerhusApi(http).GetHouseAvailabilityAsync(
            HouseId, new DateOnly(2026, 7, 1), new DateOnly(2026, 8, 31)));
        var adminAvailability = await CaptureAsync(culture, http => new AdminApiClient(http).GetHouseAvailabilityAsync(
            HouseId, new DateOnly(2026, 7, 1), new DateOnly(2026, 8, 31), CancellationToken.None));

        quote.Should().Be($"/api/houses/{HouseId}/quote?checkIn=2026-07-04&checkOut=2026-07-11&guests=4");
        availability.Should().Be($"/api/houses/{HouseId}/availability?from=2026-07-01&to=2026-08-31");
        adminAvailability.Should().Be($"/api/admin/houses/{HouseId}/availability?from=2026-07-01&to=2026-08-31");
    }

    [Fact]
    public async Task AdminLists_SendPagingInvariantly()
    {
        var houses = await CaptureAsync("da-DK", http => new AdminApiClient(http).GetHousesAsync(
            "strand", null, 12000, 25, CancellationToken.None));
        var audit = await CaptureAsync("da-DK", http => new AdminApiClient(http).GetAuditEntriesAsync(
            "House", "abc", 12000, 25, CancellationToken.None));

        houses.Should().Be("/api/admin/houses?query=strand&page=12000&pageSize=25");
        audit.Should().Be("/api/admin/audit?entity=House&entityId=abc&page=12000&pageSize=25");
    }

    private static async Task<string> CaptureAsync<T>(string culture, Func<HttpClient, Task<T>> call)
    {
        var handler = new CapturingHandler();
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://api.test/") };
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(culture);
            await call(http);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }

        return handler.RequestUri!.PathAndQuery;
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        }
    }
}
