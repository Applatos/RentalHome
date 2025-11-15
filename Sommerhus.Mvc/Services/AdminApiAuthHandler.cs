using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Sommerhus.Mvc.Infrastructure;

namespace Sommerhus.Mvc.Services;

public sealed class AdminApiAuthHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public AdminApiAuthHandler(IHttpContextAccessor httpContextAccessor)
        => this.httpContextAccessor = httpContextAccessor;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = httpContextAccessor.HttpContext?.User?.FindFirst(SommerhusClaimTypes.AdminAccessToken)?.Value;

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
