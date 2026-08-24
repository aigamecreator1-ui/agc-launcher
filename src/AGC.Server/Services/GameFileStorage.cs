using System.Net.Http.Headers;
using System.Net.Http.Json;
using AGC.Server.Configuration;

namespace AGC.Server.Services;

/// <summary>
/// Uploaded game builds and thumbnails, stored in a Supabase Storage bucket rather
/// than local disk — this app runs on a stateless host with no persistent filesystem
/// of its own. The bucket is private; every read goes through this server (using the
/// service_role key) rather than a public bucket URL, so paid-game entitlement checks
/// in GamesController still gate access to the actual file bytes, not just the DB row.
/// </summary>
public sealed class GameFileStorage(HttpClient http, AppOptions options)
{
    public async Task<string> SaveAsync(string gameId, string fileName, Stream content, CancellationToken ct)
    {
        var relativePath = $"{gameId}/{fileName}";

        using var request = new HttpRequestMessage(HttpMethod.Post, $"storage/v1/object/{options.SupabaseBucket}/{relativePath}")
        {
            Content = new StreamContent(content),
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(ContentTypeFor(fileName));
        request.Headers.Add("x-upsert", "true");

        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Supabase Storage upload failed ({(int)response.StatusCode}): {body}");
        }

        return relativePath;
    }

    /// <summary>Null if the object doesn't exist. Caller is responsible for disposing the returned stream.</summary>
    public async Task<(Stream Content, long? Length, string ContentType)?> OpenReadAsync(string relativePath, CancellationToken ct)
    {
        var response = await http.GetAsync(
            $"storage/v1/object/{options.SupabaseBucket}/{relativePath}", HttpCompletionOption.ResponseHeadersRead, ct);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            response.Dispose();
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            response.Dispose();
            throw new InvalidOperationException($"Supabase Storage download failed ({(int)response.StatusCode}): {body}");
        }

        var length = response.Content.Headers.ContentLength;
        var contentType = response.Content.Headers.ContentType?.MediaType ?? ContentTypeFor(relativePath);
        var stream = await response.Content.ReadAsStreamAsync(ct);
        return (stream, length, contentType);
    }

    /// <summary>Deletes exactly the given object paths (typically a game's build + thumbnail). Missing objects are ignored.</summary>
    public async Task DeleteAsync(IReadOnlyList<string> relativePaths, CancellationToken ct)
    {
        if (relativePaths.Count == 0)
        {
            return;
        }

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"storage/v1/object/{options.SupabaseBucket}")
        {
            Content = JsonContent.Create(new { prefixes = relativePaths }),
        };

        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Supabase Storage delete failed ({(int)response.StatusCode}): {body}");
        }
    }

    public static string ContentTypeFor(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".webp" => "image/webp",
        _ => "application/octet-stream",
    };
}
