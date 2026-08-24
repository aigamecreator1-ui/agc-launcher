using Avalonia.Media.Imaging;

namespace AGC.Launcher.Core.Services.Http;

/// <summary>Thumbnails are served unauthenticated (nothing sensitive in them), so this is a plain fetch.</summary>
public static class ThumbnailLoader
{
    private static readonly HttpClient Http = new() { BaseAddress = new Uri(ApiConfig.BaseUrl) };

    public static async Task<Bitmap?> LoadAsync(string gameId, CancellationToken ct = default)
    {
        try
        {
            await using var stream = await Http.GetStreamAsync($"api/games/{gameId}/thumbnail", ct);
            var memory = new MemoryStream();
            await stream.CopyToAsync(memory, ct);
            memory.Position = 0;
            return new Bitmap(memory);
        }
        catch
        {
            return null;
        }
    }
}
