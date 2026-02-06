using Sommerhus.Core.Dtos.Admin;

namespace Sommerhus.Mvc.Services;

public sealed class AdminAuthClient
{
    private readonly HttpClient http;

    public AdminAuthClient(HttpClient http) => this.http = http;

    public Task<ApiResponse<AdminTokenResponse?>> LoginAsync(AdminLoginRequest request, CancellationToken ct)
        => ApiHttp.PostAsync<AdminLoginRequest, AdminTokenResponse?>(http, "api/admin/auth/login", request, ct);
}
