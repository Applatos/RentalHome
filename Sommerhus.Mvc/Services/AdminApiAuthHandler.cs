using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Sommerhus.Mvc.Infrastructure;

namespace Sommerhus.Mvc.Services;

public sealed class AdminApiAuthHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly AdminAuthOptions options;
    private readonly string headerValue;

    public AdminApiAuthHandler(IHttpContextAccessor httpContextAccessor, IOptions<AdminAuthOptions> options)
    {
        this.httpContextAccessor = httpContextAccessor;
        this.options = options.Value;
        headerValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{this.options.Username}:{this.options.Password}"));
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true &&
            !string.IsNullOrWhiteSpace(options.Username) &&
            !string.IsNullOrWhiteSpace(options.Password))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", headerValue);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
