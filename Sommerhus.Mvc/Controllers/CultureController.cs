using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Sommerhus.Mvc.Controllers;

public sealed class CultureController : Controller
{
    [HttpPost("/culture/set")]
    [ValidateAntiForgeryToken]
    public IActionResult SetCulture([FromForm] string culture, [FromForm] string? returnUrl = null)
    {
        var supportedCultures = new[] { "da-DK", "en-GB" };
        if (!supportedCultures.Contains(culture, StringComparer.OrdinalIgnoreCase))
        {
            culture = "da-DK";
        }

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            });

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Houses");
    }
}
