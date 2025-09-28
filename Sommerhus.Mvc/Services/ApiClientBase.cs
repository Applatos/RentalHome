using System.Net;
using System.Net.Http.Json;

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

    protected async Task<bool> SendOk(Func<Task<HttpResponseMessage>> send)
        => (await send()).IsSuccessStatusCode;

    protected async Task<bool> SendOkOrNotFound(Func<Task<HttpResponseMessage>> send)
    {
        var res = await send();
        return res.IsSuccessStatusCode || res.StatusCode == HttpStatusCode.NotFound;
    }
}
