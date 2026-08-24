using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AGC.Launcher.Core.Models;
using AGC.Shared.Dtos;

namespace AGC.Launcher.Core.Services.Http;

/// <summary>
/// Downloads the real build the Publish flow uploaded, installs it under the user's
/// LocalAppData, and launches the resulting executable. Builds are either a single
/// .exe (copied in directly) or a .zip (extracted, then the real game exe is picked
/// out from whatever else Unity bundled alongside it).
/// </summary>
public sealed class GameInstallService : IDownloadService
{
    private static readonly string GamesRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AGC Launcher",
        "Games");

    private static readonly HashSet<string> NonGameExeNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "UnityCrashHandler32.exe",
        "UnityCrashHandler64.exe",
        "unins000.exe",
    };

    private readonly HttpClient _http;
    private readonly ISessionStore _sessionStore;
    private readonly IGameInstallStore _installStore;

    public GameInstallService(HttpClient httpClient, ISessionStore sessionStore, IGameInstallStore installStore)
    {
        _http = httpClient;
        _sessionStore = sessionStore;
        _installStore = installStore;
    }

    public bool IsInstalled(string gameId)
    {
        var record = _installStore.Find(gameId);
        if (record is null)
        {
            return false;
        }

        if (File.Exists(record.ExecutablePath))
        {
            return true;
        }

        // The exe vanished from under us (moved/deleted) — forget the stale record
        // so the UI correctly offers Download again.
        _installStore.Remove(gameId);
        return false;
    }

    public async Task InstallAsync(GameDto game, IProgress<double> progress, CancellationToken ct = default)
    {
        var gameDir = Path.Combine(GamesRoot, game.Id);
        Directory.CreateDirectory(gameDir);
        var downloadPath = Path.Combine(gameDir, $".download-{Guid.NewGuid():N}.part");

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/games/{game.Id}/download");
            var token = _sessionStore.Load()?.Token;
            if (token is not null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode)
            {
                throw new ApiException(await ReadErrorMessageAsync(response, ct));
            }

            var totalBytes = response.Content.Headers.ContentLength ?? game.BuildSizeBytes;
            var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                ?? "build.bin";

            await DownloadToFileAsync(response, downloadPath, totalBytes, progress, ct);

            var executablePath = InstallDownloadedBuild(gameDir, downloadPath, fileName);
            _installStore.Save(new InstalledGameRecord(game.Id, executablePath, DateTime.UtcNow));
            progress.Report(1.0);
        }
        catch
        {
            // Never leave a half-installed game behind — wipe everything for this
            // game so a retry starts from a clean slate.
            _installStore.Remove(game.Id);
            TryDeleteFile(downloadPath);
            TryDeleteDirectory(gameDir);
            throw;
        }
        finally
        {
            TryDeleteFile(downloadPath);
        }
    }

    public void Launch(string gameId)
    {
        var record = _installStore.Find(gameId)
            ?? throw new InvalidOperationException("This game isn't installed.");

        if (!File.Exists(record.ExecutablePath))
        {
            _installStore.Remove(gameId);
            throw new InvalidOperationException("The installed game's files are missing. Please download it again.");
        }

        Process.Start(new ProcessStartInfo(record.ExecutablePath)
        {
            WorkingDirectory = Path.GetDirectoryName(record.ExecutablePath),
            UseShellExecute = true,
        });
    }

    private static async Task DownloadToFileAsync(
        HttpResponseMessage response, string downloadPath, long totalBytes, IProgress<double> progress, CancellationToken ct)
    {
        await using var source = await response.Content.ReadAsStreamAsync(ct);
        await using var target = File.Create(downloadPath);

        var buffer = new byte[81920];
        long readTotal = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, ct)) > 0)
        {
            await target.WriteAsync(buffer.AsMemory(0, read), ct);
            readTotal += read;

            if (totalBytes > 0)
            {
                progress.Report(Math.Min(1.0, (double)readTotal / totalBytes));
            }
        }
    }

    private static string InstallDownloadedBuild(string gameDir, string downloadPath, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        if (extension == ".zip")
        {
            ClearDirectoryExcept(gameDir, downloadPath);
            ZipFile.ExtractToDirectory(downloadPath, gameDir, overwriteFiles: true);
            return FindGameExecutable(gameDir)
                ?? throw new InvalidOperationException("No executable was found inside the downloaded build.");
        }

        if (extension == ".exe")
        {
            ClearDirectoryExcept(gameDir, downloadPath);
            var finalPath = Path.Combine(gameDir, fileName);
            File.Copy(downloadPath, finalPath, overwrite: true);
            return finalPath;
        }

        throw new InvalidOperationException($"Unsupported build file type '{extension}' — expected a .zip or .exe.");
    }

    private static string? FindGameExecutable(string root)
    {
        var candidates = Directory.GetFiles(root, "*.exe", SearchOption.AllDirectories)
            .Where(p => !NonGameExeNames.Contains(Path.GetFileName(p)))
            .ToList();

        if (candidates.Count == 0)
        {
            return null;
        }

        // Prefer the shallowest match (a top-level exe over one buried in a
        // subfolder), then the largest file — Unity's real game binary dwarfs any
        // helper executables it ships alongside.
        return candidates
            .OrderBy(p => p.Count(c => c == Path.DirectorySeparatorChar))
            .ThenByDescending(p => new FileInfo(p).Length)
            .First();
    }

    private static void ClearDirectoryExcept(string directory, string keepPath)
    {
        foreach (var entry in Directory.GetFileSystemEntries(directory))
        {
            if (string.Equals(entry, keepPath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (Directory.Exists(entry))
            {
                Directory.Delete(entry, recursive: true);
            }
            else
            {
                File.Delete(entry);
            }
        }
    }

    private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ApiErrorDto>(cancellationToken: ct);
            return error?.Message ?? $"Download failed ({(int)response.StatusCode}).";
        }
        catch
        {
            return $"Download failed ({(int)response.StatusCode}).";
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best-effort cleanup — a leftover temp file doesn't affect correctness.
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                // Wipe the whole per-game folder on failure — a half-extracted zip or
                // an exe with no valid entry point found is still a corrupted install.
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup.
        }
    }
}
