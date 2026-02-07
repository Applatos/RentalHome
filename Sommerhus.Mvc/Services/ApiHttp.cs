using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace Sommerhus.Mvc.Services;

internal static class ApiHttp
{
    internal static Task<ApiResponse<T?>> GetAsync<T>(HttpClient http, string uri, CancellationToken ct)
        => SendAsync<T>(http, (client, token) => client.GetAsync(uri, token), ct);

    internal static Task<ApiResponse<TResponse?>> PostAsync<TRequest, TResponse>(HttpClient http, string uri, TRequest payload, CancellationToken ct)
        => SendAsync<TResponse>(http, (client, token) => client.PostAsJsonAsync(uri, payload, token), ct);

    internal static Task<ApiResponse<TResponse?>> PutAsync<TRequest, TResponse>(HttpClient http, string uri, TRequest payload, CancellationToken ct)
        => SendAsync<TResponse>(http, (client, token) => client.PutAsJsonAsync(uri, payload, token), ct);

    internal static Task<ApiResponse<object?>> DeleteAsync(HttpClient http, string uri, CancellationToken ct)
        => SendAsync<object>(http, (client, token) => client.DeleteAsync(uri, token), ct);

    internal static async Task<ApiResponse<T?>> SendAsync<T>(HttpClient http, Func<HttpClient, CancellationToken, Task<HttpResponseMessage>> sender, CancellationToken ct)
    {
        try
        {
            using var response = await sender(http, ct);
            return await HandleResponseAsync<T>(response, ct);
        }
        catch (TaskCanceledException ex)
        {
            return ApiResponse<T?>.Failure(ex.Message);
        }
        catch (HttpRequestException ex)
        {
            return ApiResponse<T?>.Failure($"Network error: {ex.Message}");
        }
    }

    private static async Task<ApiResponse<T?>> HandleResponseAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return ApiResponse<T?>.Success(default, response.StatusCode);
        }

        if (response.IsSuccessStatusCode)
        {
            try
            {
                var payload = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
                return ApiResponse<T?>.Success(payload, response.StatusCode);
            }
            catch (Exception ex)
            {
                return ApiResponse<T?>.Failure($"Parse error: {ex.Message}", response.StatusCode);
            }
        }

        // Read the body once, then attempt to deserialize from the string
        var body = await response.Content.ReadAsStringAsync(ct);

        if (response.StatusCode == HttpStatusCode.BadRequest || (int)response.StatusCode == 422)
        {
            return ParseValidationBody<T>(body, response.StatusCode, response.ReasonPhrase);
        }

        return ApiResponse<T?>.Failure(
            string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase ?? "Request failed" : body,
            response.StatusCode);
    }

    private static ApiResponse<T?> ParseValidationBody<T>(string body, HttpStatusCode status, string? reasonPhrase)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return ApiResponse<T?>.Failure(reasonPhrase ?? "Bad request", status);
        }

        // Try ValidationProblemDetails first (has .Errors dictionary)
        try
        {
            var validation = JsonSerializer.Deserialize<ValidationProblemDetails>(body, JsonOpts);
            if (validation?.Errors is { Count: > 0 })
            {
                var dict = new Dictionary<string, string[]>(validation.Errors, StringComparer.OrdinalIgnoreCase);
                return ApiResponse<T?>.Validation(dict, status);
            }
        }
        catch { /* not this shape, try next */ }

        // Try plain dictionary format { "field": ["msg"] }
        try
        {
            var errors = JsonSerializer.Deserialize<Dictionary<string, string[]>>(body, JsonOpts);
            if (errors is { Count: > 0 })
            {
                return ApiResponse<T?>.Validation(errors, status);
            }
        }
        catch { /* not this shape, try next */ }

        // Try ProblemDetails (has title/detail)
        try
        {
            var problem = JsonSerializer.Deserialize<ProblemDetails>(body, JsonOpts);
            if (problem is not null)
            {
                var msg = !string.IsNullOrWhiteSpace(problem.Title) ? problem.Title
                        : !string.IsNullOrWhiteSpace(problem.Detail) ? problem.Detail
                        : reasonPhrase ?? "Bad request";
                return ApiResponse<T?>.Failure(msg, status);
            }
        }
        catch { /* not this shape */ }

        // Fallback: raw text
        return ApiResponse<T?>.Failure(body, status);
    }

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);
}
