using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AGC.Shared.Dtos;

namespace AGC.Launcher.Core.Services.Http;

/// <summary>Owner-only: price suggestion and the multipart publish upload.</summary>
public sealed class OwnerPublishService(HttpClient httpClient, ISessionStore sessionStore)
{
    public async Task<decimal> SuggestPriceAsync(long buildSizeBytes, CancellationToken ct = default)
    {
        using var response = await SendAsync(
            HttpMethod.Post, "api/owner/games/suggest-price", JsonContent.Create(new SuggestPriceRequestDto(buildSizeBytes)), ct);
        await EnsureSuccessAsync(response, ct);
        var result = await response.Content.ReadFromJsonAsync<SuggestPriceResponseDto>(cancellationToken: ct);
        return result?.SuggestedPriceUsd ?? 0;
    }

    public async Task PublishAsync(
        string title,
        string description,
        string genre,
        string tags,
        bool isPaid,
        decimal priceUsd,
        string buildFilePath,
        string thumbnailFilePath,
        CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent
        {
            { new StringContent(title), "Title" },
            { new StringContent(description), "Description" },
            { new StringContent(genre), "Genre" },
            { new StringContent(tags), "Tags" },
            { new StringContent(isPaid.ToString()), "IsPaid" },
            { new StringContent(priceUsd.ToString(CultureInfo.InvariantCulture)), "PriceUsd" },
        };

        await using var buildStream = File.OpenRead(buildFilePath);
        await using var thumbStream = File.OpenRead(thumbnailFilePath);
        using var buildContent = new StreamContent(buildStream);
        using var thumbContent = new StreamContent(thumbStream);
        content.Add(buildContent, "Build", Path.GetFileName(buildFilePath));
        content.Add(thumbContent, "Thumbnail", Path.GetFileName(thumbnailFilePath));

        using var response = await SendAsync(HttpMethod.Post, "api/owner/games", content, ct);
        await EnsureSuccessAsync(response, ct);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, HttpContent content, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, path) { Content = content };
        var token = sessionStore.Load()?.Token;
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await httpClient.SendAsync(request, ct);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var error = await response.Content.ReadFromJsonAsync<ApiErrorDto>(cancellationToken: ct);
        throw new ApiException(error?.Message ?? $"Request failed ({(int)response.StatusCode}).");
    }
}
