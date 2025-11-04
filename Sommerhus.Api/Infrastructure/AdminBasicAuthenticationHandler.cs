using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Sommerhus.Api.Infrastructure;

public sealed class AdminBasicAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ISystemClock clock,
    IOptions<AdminAuthOptions> adminOptions)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder, clock)
{
    private readonly AdminAuthOptions adminOptions = adminOptions.Value;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (string.IsNullOrWhiteSpace(adminOptions.Username) || string.IsNullOrWhiteSpace(adminOptions.Password))
        {
            return Task.FromResult(AuthenticateResult.Fail("Admin credentials are not configured."));
        }

        if (!Request.Headers.TryGetValue("Authorization", out var headerValues))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing Authorization header."));
        }

        var header = headerValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization header."));
        }

        var parameter = header["Basic ".Length..].Trim();
        if (string.IsNullOrWhiteSpace(parameter))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization header."));
        }

        string decoded;
        try
        {
            var bytes = Convert.FromBase64String(parameter);
            decoded = Encoding.UTF8.GetString(bytes);
        }
        catch (FormatException)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization header."));
        }

        var separatorIndex = decoded.IndexOf(':');
        if (separatorIndex <= 0)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization header."));
        }

        var username = decoded[..separatorIndex];
        var password = decoded[(separatorIndex + 1)..];

        if (!string.Equals(username, adminOptions.Username, StringComparison.Ordinal) ||
            !string.Equals(password, adminOptions.Password, StringComparison.Ordinal))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid credentials."));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, username),
            new Claim(ClaimTypes.Name, username)
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers["WWW-Authenticate"] = "Basic realm=\"Sommerhus Admin\"";
        return base.HandleChallengeAsync(properties);
    }
}
