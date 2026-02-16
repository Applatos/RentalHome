using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;

namespace Sommerhus.Mvc.Services;

public sealed class CulturePropagationHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var cultureName = httpContextAccessor.HttpContext?
            .Features
            .Get<IRequestCultureFeature>()?
            .RequestCulture
            .UICulture
            .Name;

        if (string.IsNullOrWhiteSpace(cultureName))
        {
            cultureName = "da-DK";
        }

        if (!string.IsNullOrWhiteSpace(cultureName))
        {
            request.Headers.AcceptLanguage.Clear();
            request.Headers.AcceptLanguage.ParseAdd(cultureName);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
