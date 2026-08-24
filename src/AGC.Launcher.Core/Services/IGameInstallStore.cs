using AGC.Launcher.Core.Models;

namespace AGC.Launcher.Core.Services;

/// <summary>
/// Persists which games are installed and where, so re-opening the launcher still
/// shows "Play" instead of "Download" without re-probing the file system on every
/// game in the library.
/// </summary>
public interface IGameInstallStore
{
    InstalledGameRecord? Find(string gameId);

    void Save(InstalledGameRecord record);

    void Remove(string gameId);
}
