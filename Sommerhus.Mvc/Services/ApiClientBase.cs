using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Headers;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.HttpLogging;

namespace Sommerhus.Mvc.Services;

public class ApiResult<T>
{
    private ApiResult() { }

    public T? Data { get; private set; }
    public HttpStatusCode? StatusCode { get; init; }
    public bool Ok { get; init; }

    public string? Message { get; init; }
    public bool HasValidationErrors => Errors.Count > 0;

    public IReadOnlyDictionary<string, string[]> Errors { get; init; }

    public static ApiResult<T> Success(T? data, HttpStatusCode? status = null)
    => new() { Ok = true, Data = data, StatusCode = status };

    public static ApiResult<T> Validation(IReadOnlyDictionary<string, string[]> errors, HttpStatusCode? status = null)
        => new() { Ok = false, Errors = errors, StatusCode = status };

    public static ApiResult<T> Failure(string message, HttpStatusCode? status = null)
        => new() { Ok = false, Message = message, StatusCode = status };
}


public abstract class ApiClientBase
{
    protected readonly HttpClient _http;
    protected ApiClientBase(HttpClient http) => _http = http;

    protected Task<ApiResult<T?>> GetAsync<T>(string uri, CancellationToken ct = default)
        => SendForResultAsync<T>(ct => _http.GetAsync(uri, ct), ct);
    
    protected Task<ApiResult<TRes?>> PostAsync<TReq, TRes>(string uri, TReq body, CancellationToken ct = default)
        => SendForResultAsync<TRes>(ct => _http.PostAsJsonAsync(uri, body, ct), ct);

    protected Task<ApiResult<TRes?>> PutAsync<TReq, TRes>(string uri, TReq body, CancellationToken ct = default)
        => SendForResultAsync<TRes>(ct => _http.PutAsJsonAsync(uri, body, ct), ct);

    protected Task<ApiResult<object?>> DeleteAsync(string uri, CancellationToken ct = default)
        => SendForResultAsync<object?>(ct => _http.DeleteAsync(uri, ct), ct);

    protected Task<ApiResult<TRes?>> PostMultipartSingleAsync<TRes>(string url, string fieldName, IFormFile file, CancellationToken ct = default)
    {
        var tuple = ( Stream: file.OpenReadStream(), fileName: file.FileName, contentType: file.ContentType);
        return PostMultipartAsync<TRes>(url, fieldName, new[] { tuple }, ct);
    }

    protected async Task<ApiResult<TRes?>> PostMultipartAsync<TRes>(
        string url, 
        string fieldName, 
        IEnumerable<(Stream stream, string fileName, string? contentType)> files, 
        CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent();

        foreach (var (stream, fileName, contentType) in files)
        {
            var sc = new StreamContent(stream);
            if (!string.IsNullOrWhiteSpace(contentType))
                sc.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            form.Add(sc, fieldName, fileName);
        }
        return await SendForResultAsync<TRes>(ct => _http.PostAsync(url, form, ct), ct);
    }





    protected async Task<ApiResult<T?>> SendForResultAsync<T>(Func<CancellationToken, Task<HttpResponseMessage>> sender, CancellationToken ct = default)
    {
        try
        {
            using var response = await sender(ct);
            return await HandleResponseAsync<T>(response, ct);
        }
        catch (TaskCanceledException ex)
        {
            return ApiResult<T?>.Failure(ex.Message, status: null);
        }
        catch (HttpRequestException ex)
        {
            return ApiResult<T?>.Failure($"Network error: {ex.Message}", status: null);
        }
    }

    protected async Task<ApiResult<T?>> HandleResponseAsync<T>( HttpResponseMessage response, CancellationToken ct = default)
    {
        if (response.StatusCode == HttpStatusCode.NoContent)
            return ApiResult<T?>.Success(default, response.StatusCode);

        if (response.IsSuccessStatusCode)
        {
            try
            {
                var payload = await response.Content.ReadFromJsonAsync<T>(ct);
                return ApiResult<T?>.Success(payload, response.StatusCode);
            }
            catch (Exception ex)
            {
                return ApiResult<T?>.Failure($"Parse error: {ex.Message}", response.StatusCode);
            }
        }

        if (response.StatusCode == HttpStatusCode.BadRequest || (int)response.StatusCode == 422)
        {

            var errors = await response.Content.ReadFromJsonAsync<Dictionary<string, string[]>>(ct)
                ?? new Dictionary<string, string[]>();
            return ApiResult<T?>.Validation(errors, response.StatusCode);
        }

        var msg = await response.Content.ReadAsStringAsync(ct);
        return ApiResult<T?>.Failure(string.IsNullOrWhiteSpace(msg) ? response.ReasonPhrase ?? "Request failed" : msg, response.StatusCode);
    }

}