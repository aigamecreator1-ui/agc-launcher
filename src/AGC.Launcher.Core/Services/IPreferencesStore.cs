using AGC.Launcher.Core.Models;

namespace AGC.Launcher.Core.Services;

/// <summary>Persists basic local launcher preferences (not account data — those live server-side).</summary>
public interface IPreferencesStore
{
    AppPreferences Load();
    void Save(AppPreferences preferences);
}
