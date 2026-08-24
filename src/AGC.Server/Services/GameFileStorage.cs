namespace AGC.Server.Services;

/// <summary>
/// Local-disk storage for uploaded builds and thumbnails, keyed by game id. A real
/// production deployment would swap this for object storage (S3/Azure Blob/etc.) —
/// that's a deployment concern, not something this interface needs to change for.
/// </summary>
public sealed class GameFileStorage
{
    /// <summary>
    /// Defaults to a path relative to the dev build output; a real deployment sets
    /// STORAGE_ROOT to somewhere on persistent disk (e.g. a mounted volume) so
    /// uploaded builds survive a redeploy.
    /// </summary>
    private static readonly string RootPath = Environment.GetEnvironmentVariable("STORAGE_ROOT")
        ?? Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Storage");

    public async Task<string> SaveAsync(string gameId, string fileName, Stream content, CancellationToken ct)
    {
        var directory = Path.Combine(RootPath, gameId);
        Directory.CreateDirectory(directory);

        var relativePath = Path.Combine(gameId, fileName);
        var fullPath = Path.Combine(RootPath, relativePath);

        await using var fileStream = File.Create(fullPath);
        await content.CopyToAsync(fileStream, ct);

        return relativePath;
    }

    public string GetFullPath(string relativePath) => Path.Combine(RootPath, relativePath);

    public void DeleteGameFiles(string gameId)
    {
        var directory = Path.Combine(RootPath, gameId);
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
