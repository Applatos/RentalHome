using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace Sommerhus.Mvc.Services;

public abstract class ApiClientBase
{
    protected readonly HttpClient http;
    protected ApiClientBase(HttpClient http) => this.http = http;

    protected Task<T?> GetAsync<T>(string url, CancellationToken ct = default)
        => http.GetFromJsonAsync<T>(url, ct);

    protected async Task<IReadOnlyList<T>> GetListAsync<T>(string url, CancellationToken ct = default)
        => await http.GetFromJsonAsync<List<T>>(url, ct) ?? [];

    protected Task<bool> PutAsync<TReq>(string url, TReq body, CancellationToken ct = default)
        => SendOk(() => http.PutAsJsonAsync(url, body, ct));

    // VIGTIGT: TRes er NU nullable (TRes?)
    protected async Task<(bool ok, TRes? data)> PostAsync<TReq, TRes>(string url, TReq body, CancellationToken ct = default)
    {
        var res = await http.PostAsJsonAsync(url, body, ct);
        if (!res.IsSuccessStatusCode) return (false, default);
        var data = await res.Content.ReadFromJsonAsync<TRes>(cancellationToken: ct);
        return (true, data);
    }

    protected Task<bool> PostAsync<TReq>(string url, TReq body, CancellationToken ct = default)
        => SendOk(() => http.PostAsJsonAsync(url, body, ct));

    protected Task<bool> DeleteOkOrNotFoundAsync(string url, CancellationToken ct = default)
        => SendOkOrNotFound(() => http.DeleteAsync(url, ct));

    protected Task<ApiResult<TRes?>> PostForResultAsync<TReq, TRes>(string url, TReq body, CancellationToken ct = default)
        => SendForResultAsync<TRes>(() => http.PostAsJsonAsync(url, body, ct), ct);

    protected Task<ApiResult<object?>> PutForResultAsync<TReq>(string url, TReq body, CancellationToken ct = default)
        => SendForResultAsync<object?>(() => http.PutAsJsonAsync(url, body, ct), ct);

    protected Task<ApiResult<object?>> DeleteForResultAsync(string url, CancellationToken ct = default)
        => SendForResultAsync<object?>(() => http.DeleteAsync(url, ct), ct);

    protected async Task<bool> SendOk(Func<Task<HttpResponseMessage>> send)
        => (await send()).IsSuccessStatusCode;

    protected async Task<bool> SendOkOrNotFound(Func<Task<HttpResponseMessage>> send)
    {
        var res = await send();
        return res.IsSuccessStatusCode || res.StatusCode == HttpStatusCode.NotFound;
    }

    private async Task<ApiResult<T?>> SendForResultAsync<T>(Func<Task<HttpResponseMessage>> send, CancellationToken ct)
    {
        var res = await send();
        if (res.IsSuccessStatusCode)
        {
            T? payload = default;
            if (res.StatusCode != HttpStatusCode.NoContent &&
                res.Content.Headers.ContentLength.GetValueOrDefault() != 0)
            {
                payload = await res.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
            }

            return ApiResult<T>.Success(payload, res.StatusCode);
        }

        if (res.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity)
        {
            var errors = await ReadValidationErrorsAsync(res, ct);
            return ApiResult<T>.Validation(res.StatusCode, errors);
        }

        var message = $"API returned {(int)res.StatusCode} {res.StatusCode}";
        return ApiResult<T>.Failure(res.StatusCode, message);
    }

    private static async Task<Dictionary<string, string[]>> ReadValidationErrorsAsync(HttpResponseMessage res, CancellationToken ct)
    {
        try
        {
            var validation = await res.Content.ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken: ct);
            if (validation?.Errors?.Count > 0)
            {
                return validation.Errors.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value ?? Array.Empty<string>());
            }
        }
        catch
        {
            // ignored
        }

        try
        {
            var problem = await res.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: ct);
            if (problem is not null)
            {
                var message = !string.IsNullOrWhiteSpace(problem.Detail)
                    ? problem.Detail
                    : problem.Title ?? $"Valideringsfejl ({(int)res.StatusCode})";
                return new Dictionary<string, string[]> { { string.Empty, new[] { message } } };
            }
        }
        catch
        {
            // ignored
        }

        return new Dictionary<string, string[]>
        {
            { string.Empty, new[] { $"Valideringsfejl ({(int)res.StatusCode})" } }
        };
    }
}
