using AGC.Shared.Dtos;

namespace AGC.Launcher.Core.Services;

/// <summary>
/// Downloads a purchased game's real build from the backend, installs it to local
/// disk, and launches the installed executable as its own process.
/// </summary>
public interface IDownloadService
{
    /// <summary>True if this game has a locally installed, verified-present executable.</summary>
    bool IsInstalled(string gameId);

    /// <summary>
    /// Streams the build to disk with real byte-for-byte progress, then installs it
    /// (extracting a zip or placing a standalone .exe) into a per-game folder. On any
    /// failure, partial state is cleaned up so the game correctly falls back to
    /// "Download" and can be retried.
    /// </summary>
    Task InstallAsync(GameDto game, IProgress<double> progress, CancellationToken ct = default);

    /// <summary>Launches the installed game's executable as an independent process.</summary>
    void Launch(string gameId);
}
