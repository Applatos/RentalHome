using Sommerhus.Core.Dtos.Auth;

namespace Sommerhus.Mvc.Services;

public sealed class PublicAuthClient
{
    private readonly HttpClient http;

    public PublicAuthClient(HttpClient http) => this.http = http;

    public Task<ApiResponse<AuthTokenResponse?>> RegisterAsync(RegisterRequest request, CancellationToken ct)
        => ApiHttp.PostAsync<RegisterRequest, AuthTokenResponse?>(http, "api/auth/register", request, ct);

    public Task<ApiResponse<AuthTokenResponse?>> LoginAsync(LoginRequest request, CancellationToken ct)
        => ApiHttp.PostAsync<LoginRequest, AuthTokenResponse?>(http, "api/auth/login", request, ct);
}
