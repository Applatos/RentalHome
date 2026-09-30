using System.Globalization;
using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Mvc;
using Sommerhus.Mvc.Infrastructure;
using Sommerhus.Mvc.Services;

namespace Sommerhus.Api.Tests.Mvc.Pages;

public sealed class PricingErrorTextTests
{
    private static readonly IStringLocalizer Localizer = new ResourceManagerStringLocalizerFactory(
            Options.Create(new LocalizationOptions { ResourcesPath = "Resources" }),
            NullLoggerFactory.Instance)
        .Create(typeof(SharedResource));

    private static IReadOnlyDictionary<string, string[]> Errors(params string[] keys)
        => keys.ToDictionary(k => k, _ => new[] { "English text from the API" });

    [Theory]
    [InlineData(PricingErrors.UnpricedNights, PricingErrors.UnpricedNights)]
    [InlineData(PricingErrors.Guests, PricingErrors.Guests)]
    [InlineData("Guests", PricingErrors.Guests)]
    [InlineData(PricingErrors.Dates, PricingErrors.Dates)]
    [InlineData("Departure", PricingErrors.Dates)]
    [InlineData("checkIn", PricingErrors.Dates)]
    [InlineData("houseId", PricingErrorText.Unknown)]
    public void Classify_ValidationKey_NamesTheFailure(string key, string expected)
        => PricingErrorText.Classify(HttpStatusCode.BadRequest, Errors(key)).Should().Be(expected);

    [Fact]
    public void Classify_Conflict_IsUnavailableWhateverTheBody()
        => PricingErrorText.Classify(HttpStatusCode.Conflict, Errors(PricingErrors.Dates)).Should().Be(PricingErrorText.Unavailable);

    [Fact]
    public void Classify_SeveralKeys_DatesBeforeGuestsBeforeUnpricedNights()
    {
        PricingErrorText.Classify(HttpStatusCode.BadRequest, Errors(PricingErrors.UnpricedNights, PricingErrors.Guests, PricingErrors.Dates))
            .Should().Be(PricingErrors.Dates);
        PricingErrorText.Classify(HttpStatusCode.BadRequest, Errors(PricingErrors.UnpricedNights, PricingErrors.Guests))
            .Should().Be(PricingErrors.Guests);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.NotFound)]
    public void Classify_NoValidationErrors_IsUnknown(HttpStatusCode status)
        => PricingErrorText.Classify(status, new Dictionary<string, string[]>()).Should().Be(PricingErrorText.Unknown);

    [Fact]
    public void Describe_InDanish_UsesTheResourceTextsWithDanishLetters()
    {
        using var _ = new UiCulture("da-DK");

        Describe(HttpStatusCode.BadRequest, Errors(PricingErrors.UnpricedNights), PricingErrorContext.Quote)
            .Should().Be("Der er endnu ikke fastsat en pris for alle nætter i perioden. Vælg andre datoer.");
        Describe(HttpStatusCode.BadRequest, Errors(PricingErrors.Guests), PricingErrorContext.Booking, maxGuests: 6)
            .Should().Be("Antallet af gæster passer ikke til huset (højst 6).");
        Describe(HttpStatusCode.BadRequest, Errors(PricingErrors.Guests), PricingErrorContext.Booking)
            .Should().Be("Antallet af gæster passer ikke til huset.");
        Describe(HttpStatusCode.Conflict, Errors(), PricingErrorContext.Booking)
            .Should().Be("De valgte datoer er ikke ledige.");
    }

    [Fact]
    public void Describe_InEnglish_UsesTheEnglishResources()
    {
        using var _ = new UiCulture("en-GB");

        Describe(HttpStatusCode.Conflict, Errors(), PricingErrorContext.Quote)
            .Should().Be("The selected dates are not available.");
        Describe(HttpStatusCode.BadRequest, Errors(PricingErrors.Guests), PricingErrorContext.Quote, maxGuests: 4)
            .Should().Be("The number of guests does not fit this house (at most 4).");
    }

    [Fact]
    public void Describe_UnknownFailure_UsesTheContextsGenericTextAndNeverTheApiMessage()
    {
        using var _ = new UiCulture("da-DK");
        var failure = ApiResponse<PriceQuoteResponseDto?>.Failure("{\"title\":\"Service Unavailable\",\"status\":503}", HttpStatusCode.ServiceUnavailable);

        var quote = PricingErrorText.Describe(failure, Localizer, PricingErrorContext.Quote);
        var booking = PricingErrorText.Describe(failure, Localizer, PricingErrorContext.Booking);

        quote.Code.Should().Be(PricingErrorText.Unknown);
        quote.Text.Should().Be("Prisen kunne ikke beregnes lige nu. Prøv igen senere.");
        booking.Text.Should().Be("Bookingen kunne ikke gennemføres lige nu. Prøv igen senere.");
    }

    [Fact]
    public void Describe_ValidationResponse_ReadsTheKeysOfTheApiResponse()
    {
        using var _ = new UiCulture("da-DK");
        var response = ApiResponse<BookingDto?>.Validation(Errors(PricingErrors.UnpricedNights), HttpStatusCode.BadRequest);

        var message = PricingErrorText.Describe(response, Localizer, PricingErrorContext.Booking);

        message.Code.Should().Be(PricingErrors.UnpricedNights);
        message.Text.Should().NotContain("English");
    }

    private static string Describe(HttpStatusCode status, IReadOnlyDictionary<string, string[]> errors, PricingErrorContext context, int? maxGuests = null)
        => PricingErrorText.Describe(status, errors, Localizer, context, maxGuests).Text;

    private sealed class UiCulture : IDisposable
    {
        private readonly CultureInfo previous = CultureInfo.CurrentUICulture;

        public UiCulture(string name) => CultureInfo.CurrentUICulture = new CultureInfo(name);

        public void Dispose() => CultureInfo.CurrentUICulture = previous;
    }
}
