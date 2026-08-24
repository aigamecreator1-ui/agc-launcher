using System.Net.Http.Headers;
using System.Net.Http.Json;
using AGC.Shared.Dtos;

namespace AGC.Launcher.Core.Services.Http;

/// <summary>
/// Thin wrapper around HttpClient for talking to AGC.Server: attaches the bearer
/// token when present, and turns non-success responses into ApiException with the
/// server's own error message.
/// </summary>
public sealed class ApiClient(HttpClient http, ISessionStore sessionStore)
{
    public Task<TResponse> PostAsync<TResponse>(string path, object body, CancellationToken ct = default)
        => SendAsync<TResponse>(HttpMethod.Post, path, body, ct);

    public Task<TResponse> GetAsync<TResponse>(string path, CancellationToken ct = default)
        => SendAsync<TResponse>(HttpMethod.Get, path, null, ct);

    public async Task DeleteAsync(string path, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, path);

        var token = sessionStore.Load()?.Token;
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ApiErrorDto>(cancellationToken: ct);
            throw new ApiException(error?.Message ?? $"Request failed ({(int)response.StatusCode}).");
        }
    }

    private async Task<TResponse> SendAsync<TResponse>(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        var token = sessionStore.Load()?.Token;
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        using var response = await http.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ApiErrorDto>(cancellationToken: ct);
            throw new ApiException(error?.Message ?? $"Request failed ({(int)response.StatusCode}).");
        }

        var result = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: ct);
        return result ?? throw new ApiException("The server returned an empty response.");
    }
}
