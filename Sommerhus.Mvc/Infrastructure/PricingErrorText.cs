using System.Net;
using Microsoft.Extensions.Localization;
using Sommerhus.Mvc.Services;

namespace Sommerhus.Mvc.Infrastructure;

/// <summary>
/// Where a pricing error is shown. It decides the text used for a failure the visitor cannot act on.
/// </summary>
public enum PricingErrorContext
{
    Quote,
    Booking
}

/// <summary>
/// A failed quote or booking, classified by <see cref="PricingErrorText.Classify"/>, with the text to show.
/// </summary>
public sealed record PricingErrorMessage(string Code, string Text);

/// <summary>
/// Turns a failed quote or booking response from the API into localized text. The API names the
/// failure with the validation keys in <see cref="PricingErrors"/> and answers a date conflict with
/// 409. Its own messages are English fallbacks and are never shown to visitors.
/// </summary>
public static class PricingErrorText
{
    /// <summary>The dates are valid but not available (409).</summary>
    public const string Unavailable = "unavailable";

    /// <summary>Any other failure.</summary>
    public const string Unknown = "unknown";

    // Older API versions name the date field itself instead of using the shared key.
    private static readonly string[] DateKeys = [PricingErrors.Dates, "arrival", "departure", "checkIn", "checkOut"];

    public static PricingErrorMessage Describe<T>(
        ApiResponse<T> response,
        IStringLocalizer localizer,
        PricingErrorContext context,
        int? maxGuests = null)
        => Describe(response.StatusCode, response.Errors, localizer, context, maxGuests);

    public static PricingErrorMessage Describe(
        HttpStatusCode? status,
        IReadOnlyDictionary<string, string[]>? errors,
        IStringLocalizer localizer,
        PricingErrorContext context,
        int? maxGuests = null)
    {
        var code = Classify(status, errors);
        var text = code switch
        {
            PricingErrors.Dates => localizer["Pages.PricingError.DatesInvalid"],
            PricingErrors.Guests when maxGuests is > 0 => localizer["Pages.PricingError.GuestsMax", maxGuests.Value],
            PricingErrors.Guests => localizer["Pages.PricingError.Guests"],
            PricingErrors.UnpricedNights => localizer["Pages.PricingError.UnpricedNights"],
            Unavailable => localizer["Pages.PricingError.DatesUnavailable"],
            _ when context == PricingErrorContext.Booking => localizer["Pages.PricingError.BookingFailed"],
            _ => localizer["Pages.PricingError.QuoteFailed"]
        };

        return new PricingErrorMessage(code, text.Value);
    }

    /// <summary>
    /// Returns one of <see cref="PricingErrors.Dates"/>, <see cref="PricingErrors.Guests"/>,
    /// <see cref="PricingErrors.UnpricedNights"/>, <see cref="Unavailable"/> or <see cref="Unknown"/>.
    /// When several keys are present, the one the visitor should fix first wins: dates, then guests,
    /// then unpriced nights.
    /// </summary>
    public static string Classify(HttpStatusCode? status, IReadOnlyDictionary<string, string[]>? errors)
    {
        if (status == HttpStatusCode.Conflict)
        {
            return Unavailable;
        }

        if (errors is null || errors.Count == 0)
        {
            return Unknown;
        }

        if (DateKeys.Any(key => HasKey(errors, key)))
        {
            return PricingErrors.Dates;
        }

        if (HasKey(errors, PricingErrors.Guests))
        {
            return PricingErrors.Guests;
        }

        if (HasKey(errors, PricingErrors.UnpricedNights))
        {
            return PricingErrors.UnpricedNights;
        }

        return Unknown;
    }

    // The error dictionary is case-insensitive when it comes from a validation problem, but not
    // necessarily otherwise, and the API may name a field in PascalCase.
    private static bool HasKey(IReadOnlyDictionary<string, string[]> errors, string key)
        => errors.Keys.Any(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
}
